using Importer.Api.Storage;
using Importer.Core.Config;
using Microsoft.Extensions.Options;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOptions<ApiConfig>()
    .Bind(builder.Configuration.GetSection("ApiConfig"))
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageEndpoint), "StorageEndpoint obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageBucket), "StorageBucket obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageAccessKey), "StorageAccessKey obrigatório")
    .Validate(c => !string.IsNullOrWhiteSpace(c.StorageSecretKey), "StorageSecretKey obrigatório")
    .ValidateOnStart();

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

builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("dev");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
