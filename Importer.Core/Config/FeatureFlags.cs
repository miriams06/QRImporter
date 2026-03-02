namespace Importer.Core.Config
{
    /// <summary>
    /// Flags de funcionalidade controladas por ambiente (appsettings) ou overrides em runtime (DevTools).
    /// 
    /// Critérios da doc (Módulo 0.2):
    /// - Sync ativo/inativo
    /// - Modo offline forçado (para testes sem desligar internet)
    /// - Reprocessamento QR
    /// </summary>
    public class FeatureFlags
    {
        /// <summary>
        /// Força o modo offline mesmo que haja conectividade.
        /// Útil para testar o fluxo offline sem desligar a rede.
        /// Default: false
        /// </summary>
        public bool OfflineMode { get; set; } = false;

        /// <summary>
        /// Ativa/desativa a sincronização automática com a API quando online.
        /// Se false, os documentos ficam apenas no IndexedDB.
        /// Default: true
        /// </summary>
        public bool ServerSyncEnabled { get; set; } = true;

        /// <summary>
        /// Ativa o botão e pipeline de reprocessamento de QR no ecrã de validação.
        /// Default: true
        /// </summary>
        public bool QrReprocessingEnabled { get; set; } = true;

        /// <summary>
        /// Ativa o modo de captura por câmara (PWA).
        /// Pode ser desligado em ambientes desktop-only.
        /// Default: true
        /// </summary>
        public bool CameraEnabled { get; set; } = true;

        /// <summary>
        /// Mostra métricas de performance (t_pdf_render, t_qr_decode, etc.) no UI.
        /// Útil para testes de performance (Módulo K2).
        /// Default: false (só ligado em Development)
        /// </summary>
        public bool ShowPerformanceMetrics { get; set; } = false;
    }
}
