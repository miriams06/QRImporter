using Importer.Client;
using Importer.Client.Auth;
using Importer.Client.Services;
using Importer.Core.Config;
using Microsoft.AspNetCore.Components.Authorization; // NOVO
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── AppConfig (existente — sem alterações) ──────────────────────────────────
builder.Services.AddOptions<AppConfig>()
    .Bind(builder.Configuration.GetSection("AppConfig"))
    .Validate(c => !string.IsNullOrWhiteSpace(c.ApiBaseUrl), "AppConfig.ApiBaseUrl é obrigatório.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppConfig>>().Value);

// ── HttpClient para a API (existente — sem alterações) ─────────────────────
builder.Services.AddHttpClient<ApiClient>((sp, http) =>
{
    var cfg = sp.GetRequiredService<AppConfig>();
    http.BaseAddress = new Uri(cfg.ApiBaseUrl, UriKind.Absolute);
});
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// ── Auth: ClientAuthService como AuthenticationStateProvider (NOVO) ─────────
// Singleton porque o estado de auth tem de ser partilhado em toda a app
builder.Services.AddSingleton<ClientAuthService>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ClientAuthService>());

// Necessário para <AuthorizeView>, [Authorize], etc. no Blazor WASM
builder.Services.AddAuthorizationCore(opt =>
{
    opt.AddPolicy("AdminOnly",       p => p.RequireRole("Admin"));
    opt.AddPolicy("OperadorOrAbove", p => p.RequireRole("Admin", "Operador"));
    opt.AddPolicy("RevisorOrAbove",  p => p.RequireRole("Admin", "Operador", "Revisor"));
});

// ── AuthState (existente — mantido para retrocompatibilidade) ───────────────
// ClientAuthService substitui AuthState funcionalmente, mas mantemos o registo
// para não partir código que já usa AuthState directamente.
builder.Services.AddScoped<AuthState>();

// ── Restantes serviços (existentes — sem alterações) ───────────────────────
builder.Services.AddSingleton<DocumentStateService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<FeatureFlagsService>();
builder.Services.AddScoped<OnlineModeService>();
builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<SyncAutoRunner>();
builder.Services.AddScoped<PdfRenderService>();
builder.Services.AddScoped<QrDecodeService>();

// ════════════════════════════════════════════════════════════════════════════
var host = builder.Build();

// ── Tentar restaurar sessão ao arrancar (NOVO) ──────────────────────────────
// Se o utilizador já tinha um cookie de refresh válido, restaura a sessão
// em silêncio sem pedir login outra vez.
var auth = host.Services.GetRequiredService<ClientAuthService>();
await auth.TryRefreshAsync();

await host.RunAsync();
