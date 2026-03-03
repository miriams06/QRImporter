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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// ── Controllers + Swagger (existente) ──────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── ApiConfig (existente + validações JWT e DB adicionadas) ────────────────
builder.Services.AddOptions<ApiConfig>()
    .Bind(builder.Configuration.GetSection("ApiConfig"))
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageEndpoint),         "StorageEndpoint obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageBucket),           "StorageBucket obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageAccessKey),        "StorageAccessKey obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageSecretKey),        "StorageSecretKey obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.JwtSecret),               "JwtSecret obrigatório")          // NOVO
    .Validate(c => !string.IsNullOrWhiteSpace(c.DatabaseConnectionString), "DatabaseConnectionString obrigatório") // NOVO
    .ValidateOnStart();

// Singleton para injecção directa (sem IOptions<>) — padrão já existente
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiConfig>>().Value);

// ── MinIO (existente — sem alterações) ─────────────────────────────────────
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ApiConfig>>().Value;
    // Endpoint no formato "host:porta" (sem http://)
    var minio = new MinioClient()
        .WithEndpoint(cfg.StorageEndpoint)
        .WithCredentials(cfg.StorageAccessKey, cfg.StorageSecretKey);
    if (cfg.StorageUseSsl)
        minio = minio.WithSSL();
    return minio.Build();
});
builder.Services.AddScoped<IStorageService, S3StorageService>();

// ── PostgreSQL + EF Core (NOVO) ─────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    var cfg = sp.GetRequiredService<ApiConfig>();
    opt.UseNpgsql(cfg.DatabaseConnectionString);
});

// ── JWT Authentication (NOVO) ───────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var section  = builder.Configuration.GetSection("ApiConfig");
        var secret   = section["JwtSecret"]   ?? throw new InvalidOperationException("JwtSecret em falta");
        var issuer   = section["JwtIssuer"]   ?? "importer-api";
        var audience = section["JwtAudience"] ?? "importer-client";

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });

// ── RBAC (NOVO) ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly",       p => p.RequireRole("Admin"));
    opt.AddPolicy("OperadorOrAbove", p => p.RequireRole("Admin", "Operador"));
    opt.AddPolicy("RevisorOrAbove",  p => p.RequireRole("Admin", "Operador", "Revisor"));
});

// ── Serviços de Auth (NOVO) ─────────────────────────────────────────────────
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();

// ── CORS (existente — sem alterações) ──────────────────────────────────────
builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// ════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

app.UseSerilogRequestLogging();

// ── Migrations automáticas em Development (NOVO) ───────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Pipeline HTTP ───────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");
app.UseHttpsRedirection();
app.UseAuthentication(); // NOVO — tem de vir ANTES de UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
