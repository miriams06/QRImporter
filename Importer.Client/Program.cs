using Importer.Client;
using Importer.Client.Auth;
using Importer.Client.Services;
using Importer.Core.Config;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions<AppConfig>()
    .Bind(builder.Configuration.GetSection("AppConfig"))
    .Validate(c => !string.IsNullOrWhiteSpace(c.ApiBaseUrl), "AppConfig.ApiBaseUrl é obrigatório.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppConfig>>().Value);

builder.Services.AddHttpClient<ApiClient>((sp, http) =>
{
    var cfg = sp.GetRequiredService<AppConfig>();
    http.BaseAddress = new Uri(cfg.ApiBaseUrl, UriKind.Absolute);
});
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<ClientAuthService>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ClientAuthService>());

builder.Services.AddAuthorizationCore(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opt.AddPolicy("OperadorOrAbove", p => p.RequireRole("Admin", "Operador"));
    opt.AddPolicy("RevisorOrAbove", p => p.RequireRole("Admin", "Operador", "Revisor"));
});

builder.Services.AddScoped<AuthState>();

builder.Services.AddSingleton<DocumentStateService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<FeatureFlagsService>();
builder.Services.AddScoped<OnlineModeService>();
builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<SyncAutoRunner>();
builder.Services.AddScoped<PdfRenderService>();
builder.Services.AddScoped<QrDecodeService>();
builder.Services.AddScoped<CompanyLookupService>();

var host = builder.Build();

var auth = host.Services.GetRequiredService<ClientAuthService>();
await auth.TryRefreshAsync();

await host.RunAsync();