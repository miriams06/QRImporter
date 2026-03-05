using System.Text;
using Importer.Api.Auth;
using Importer.Api.Data;
using Importer.Api.Storage;
using Importer.Core.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<ApiConfig>()
    .Bind(builder.Configuration.GetSection("ApiConfig"))
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageEndpoint), "StorageEndpoint obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageBucket), "StorageBucket obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageAccessKey), "StorageAccessKey obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageSecretKey), "StorageSecretKey obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.JwtSecret), "JwtSecret obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.DatabaseConnectionString), "DatabaseConnectionString obrigatório")
    .ValidateOnStart();

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiConfig>>().Value);

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiConfig>>().Value;
    var minio = new MinioClient()
        .WithEndpoint(cfg.StorageEndpoint)
        .WithCredentials(cfg.StorageAccessKey, cfg.StorageSecretKey);

    if (cfg.StorageUseSsl)
        minio = minio.WithSSL();

    return minio.Build();
});

builder.Services.AddScoped<IStorageService, S3StorageService>();

builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    var cfg = sp.GetRequiredService<ApiConfig>();

    if (!TryResolveDatabaseConnectionString(cfg, builder.Configuration, out var raw))
    {
        throw new InvalidOperationException(
            "DatabaseConnectionString não está configurada. Defina ApiConfig__DatabaseConnectionString " +
            "ou ConnectionStrings__DefaultConnection com uma connection string PostgreSQL válida.");
    }

    try
    {
        var parsed = new NpgsqlConnectionStringBuilder(raw);
        opt.UseNpgsql(parsed.ConnectionString);
    }
    catch (ArgumentException ex)
    {
        throw new InvalidOperationException(
            "DatabaseConnectionString inválida. Formato esperado: " +
            "Host=<db-host>;Port=<db-port>;Database=<db-name>;Username=<db-user>;Password=<db-password>",
            ex);
    }
});

static bool LooksLikePlaceholder(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return false;
    var trimmed = value.Trim();
    return trimmed.StartsWith("#{") && trimmed.EndsWith("}#");
}

static bool TryResolveDatabaseConnectionString(ApiConfig cfg, IConfiguration configuration, out string connectionString)
{
    var raw = cfg.DatabaseConnectionString;

    if (LooksLikePlaceholder(raw))
        raw = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

    connectionString = raw;
    return !string.IsNullOrWhiteSpace(raw) && !LooksLikePlaceholder(raw);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var section = builder.Configuration.GetSection("ApiConfig");
        var secret = section["JwtSecret"] ?? throw new InvalidOperationException("JwtSecret em falta");
        var issuer = section["JwtIssuer"] ?? "importer-api";
        var audience = section["JwtAudience"] ?? "importer-client";

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opt.AddPolicy("OperadorOrAbove", p => p.RequireRole("Admin", "Operador"));
    opt.AddPolicy("RevisorOrAbove", p => p.RequireRole("Admin", "Operador", "Revisor"));
});

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p =>
        p.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (TryResolveDatabaseConnectionString(
        app.Services.GetRequiredService<ApiConfig>(),
        app.Configuration,
        out _))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var migrations = db.Database.GetMigrations();

    if (migrations.Any())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    // Fallback robusto para bases já existentes criadas sem a tabela Documents.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Documents" (
            "Id" uuid NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "ReceivedAtUtc" timestamp with time zone NOT NULL,
            "Source" character varying(20) NOT NULL,
            "DocumentType" character varying(40) NOT NULL,
            "Series" character varying(40) NOT NULL,
            "DocumentNumber" character varying(60) NOT NULL,
            "DocumentDate" timestamp with time zone NULL,
            "IssuerTaxId" character varying(30) NOT NULL,
            "IssuerName" character varying(200) NOT NULL,
            "Atcud" character varying(120) NOT NULL,
            "QrRawPayload" text NOT NULL,
            "QrParsedJson" text NOT NULL,
            "SyncStatus" character varying(30) NOT NULL,
            "StorageObjectKey" character varying(500) NOT NULL,
            "StorageMetadataKey" character varying(500) NOT NULL,
            "OriginalFileName" character varying(255) NOT NULL,
            "FileContentType" character varying(120) NOT NULL,
            "FileSize" bigint NOT NULL,
            "MetadataJson" text NOT NULL,
            CONSTRAINT "PK_Documents" PRIMARY KEY ("Id")
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_Documents_CreatedAtUtc" ON "Documents" ("CreatedAtUtc");
        CREATE INDEX IF NOT EXISTS "IX_Documents_DocumentDate" ON "Documents" ("DocumentDate");
        CREATE INDEX IF NOT EXISTS "IX_Documents_IssuerTaxId" ON "Documents" ("IssuerTaxId");
        CREATE INDEX IF NOT EXISTS "IX_Documents_Atcud" ON "Documents" ("Atcud");
        CREATE INDEX IF NOT EXISTS "IX_Documents_SyncStatus" ON "Documents" ("SyncStatus");
        """);
}
else
{
    app.Logger.LogWarning(
        "A saltar inicialização da base de dados porque DatabaseConnectionString não está definida. " +
        "Configure ApiConfig__DatabaseConnectionString ou ConnectionStrings__DefaultConnection.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
