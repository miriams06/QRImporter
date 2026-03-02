using System.Text.Json;
using Importer.Core.Config;
using Microsoft.JSInterop;

namespace Importer.Client.Services;

/// <summary>
/// Serviço de feature flags com suporte a overrides em runtime (via DevTools).
/// 
/// Prioridade: Override (localStorage) > AppConfig (appsettings.json)
/// </summary>
public sealed class FeatureFlagsService
{
    private const string StorageKey = "importer:featureFlags:overrides";

    private readonly AppConfig _cfg;
    private readonly IJSRuntime _js;

    public FeatureFlagOverrides Overrides { get; private set; } = new();

    public FeatureFlagsService(AppConfig cfg, IJSRuntime js)
    {
        _cfg = cfg;
        _js = js;
    }

    // --- Flags efetivas (override se existir, senão AppConfig) ---

    public bool EffectiveOfflineMode =>
        Overrides.OfflineMode ?? _cfg.FeatureFlags.OfflineMode;

    public bool EffectiveServerSyncEnabled =>
        Overrides.ServerSyncEnabled ?? _cfg.FeatureFlags.ServerSyncEnabled;

    public bool EffectiveQrReprocessingEnabled =>
        Overrides.QrReprocessingEnabled ?? _cfg.FeatureFlags.QrReprocessingEnabled;

    public bool EffectiveCameraEnabled =>
        Overrides.CameraEnabled ?? _cfg.FeatureFlags.CameraEnabled;

    public bool EffectiveShowPerformanceMetrics =>
        Overrides.ShowPerformanceMetrics ?? _cfg.FeatureFlags.ShowPerformanceMetrics;

    // --- Persistência ---

    /// <summary>
    /// Carrega overrides guardados no localStorage (chamado no arranque da app).
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
                Overrides = JsonSerializer.Deserialize<FeatureFlagOverrides>(json) ?? new();
        }
        catch
        {
            // Se o localStorage falhar (ex: modo privado restrito), ignora silenciosamente.
            Overrides = new();
        }
    }

    /// <summary>
    /// Persiste os overrides atuais no localStorage.
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Overrides);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch { /* ignora falhas de storage */ }
    }

    /// <summary>
    /// Remove todos os overrides e volta aos valores do AppConfig.
    /// </summary>
    public async Task ClearAsync()
    {
        Overrides = new();
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch { }
    }
}
