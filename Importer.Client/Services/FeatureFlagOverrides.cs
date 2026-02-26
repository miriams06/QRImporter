namespace Importer.Client.Services
{
    // Overrides guardados no browser (localStorage).
    // null => não há override (usa AppConfig).
    public sealed class FeatureFlagOverrides
    {
        public bool? OfflineMode { get; set; }
        public bool? ServerSyncEnabled { get; set; }
        public bool? QrReprocessingEnabled { get; set; }
    }
}
