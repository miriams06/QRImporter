using System.Text.Json;
using Importer.Core.Config;
using Microsoft.JSInterop;

namespace Importer.Client.Services;

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

    // Flags "efetivas": override (se existir) senão AppConfig
    public bool EffectiveOfflineMode =>
        Overrides.OfflineMode ?? _cfg.FeatureFlags.OfflineMode;

    public bool EffectiveServerSyncEnabled =>
        Overrides.ServerSyncEnabled ?? _cfg.FeatureFlags.ServerSyncEnabled;

    public bool EffectiveQrReprocessingEnabled =>
        Overrides.QrReprocessingEnabled ?? _cfg.FeatureFlags.QrReprocessingEnabled;

    public async Task LoadAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            Overrides = JsonSerializer.Deserialize<FeatureFlagOverrides>(json) ?? new();
        }
        catch
        {
            Overrides = new(); // se estiver corrompido, ignora
        }
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Overrides);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearAsync()
    {
        Overrides = new();
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}
