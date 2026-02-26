namespace Importer.Client.Services;

public sealed class OnlineModeService
{
    private readonly ConnectivityService _conn;
    private readonly FeatureFlagsService _flags;

    public OnlineModeService(ConnectivityService conn, FeatureFlagsService flags)
    {
        _conn = conn;
        _flags = flags;
    }

    public async Task<bool> IsEffectivelyOnlineAsync()
    {
        // Se estiver “Forçar Offline”, então é offline sempre
        if (_flags.EffectiveOfflineMode)
            return false;

        return await _conn.IsOnlineAsync();
    }
}
