namespace Importer.Core.Config
{
    /// <summary>
    /// Configuração pública do Client (Blazor WASM).
    /// Vai para o browser via wwwroot/appsettings.json.
    /// 
    /// REGRA: Nunca colocar segredos aqui (passwords, chaves privadas).
    /// Apenas config pública e URLs de API/storage necessárias ao frontend.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// URL base da API. Ex: https://api.importer.local/
        /// Obrigatório. Não pode ser hardcoded no código.
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint do storage (MinIO/S3) acessível pelo client (se aplicável).
        /// Pode estar vazio se o client nunca aceder directamente ao storage.
        /// </summary>
        public string StorageEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Nome do bucket de ficheiros.
        /// </summary>
        public string StorageBucket { get; set; } = string.Empty;

        /// <summary>
        /// Nome do ambiente atual. Ex: Development, Staging, Production.
        /// Usado para mostrar DevTools apenas em Development.
        /// </summary>
        public string EnvironmentName { get; set; } = "Development";

        /// <summary>
        /// Feature flags controláveis por ambiente.
        /// </summary>
        public FeatureFlags FeatureFlags { get; set; } = new();
    }
}
