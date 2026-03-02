namespace Importer.Client.Services
{
    /// <summary>
    /// Overrides de feature flags guardados no browser (localStorage).
    /// null significa "sem override" — usa o valor do AppConfig.
    /// 
    /// Usado pelo DevTools para sobrepor flags em runtime sem alterar config.
    /// </summary>
    public sealed class FeatureFlagOverrides
    {
        public bool? OfflineMode { get; set; }
        public bool? ServerSyncEnabled { get; set; }
        public bool? QrReprocessingEnabled { get; set; }
        public bool? CameraEnabled { get; set; }
        public bool? ShowPerformanceMetrics { get; set; }
    }
}
