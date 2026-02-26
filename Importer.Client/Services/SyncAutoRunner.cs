namespace Importer.Client.Services;

/// <summary>
/// Faz sync automático quando:
/// - o browser passa a online
/// - e não está "Forçar Offline"
/// - e ServerSyncEnabled está ligado
/// </summary>
public sealed class SyncAutoRunner
{
    private readonly ConnectivityService _conn;
    private readonly OnlineModeService _online;
    private readonly FeatureFlagsService _flags;
    private readonly SyncService _sync;

    private bool _wired;

    public SyncAutoRunner(
        ConnectivityService conn,
        OnlineModeService online,
        FeatureFlagsService flags,
        SyncService sync)
    {
        _conn = conn;
        _online = online;
        _flags = flags;
        _sync = sync;
    }

    public async Task WireOnceAsync()
    {
        if (_wired) return;
        _wired = true;

        _conn.OnlineChanged += async (isOnline) =>
        {
            if (!isOnline) return;
            await TrySyncAsync();
        };

        // “kick” inicial: se já está online quando arrancas
        if (await _online.IsEffectivelyOnlineAsync())
            await TrySyncAsync();
    }

    private async Task TrySyncAsync()
    {
        if (!_flags.EffectiveServerSyncEnabled) return;
        if (!await _online.IsEffectivelyOnlineAsync()) return;
        await _sync.SyncPendingAsync();
    }
}