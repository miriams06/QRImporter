namespace Importer.Core.Config
{
    /// <summary>
    /// Configuração privada do Backend (API).
    /// NUNCA vai para o browser.
    /// 
    /// Lida via IOptions&lt;ApiConfig&gt; nos controllers/serviços do servidor.
    /// Valores sensíveis (credenciais) devem vir de variáveis de ambiente ou secrets,
    /// NUNCA hardcoded no appsettings.json que vai para o repositório.
    /// </summary>
    public class ApiConfig
    {
        // --- Storage (MinIO / S3) ---

        /// <summary>
        /// Endpoint do MinIO. Ex: http://minio:9000
        /// Em produção, usar variável de ambiente: ApiConfig__StorageEndpoint
        /// </summary>
        public string StorageEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Nome do bucket onde os ficheiros são guardados.
        /// </summary>
        public string StorageBucket { get; set; } = string.Empty;

        /// <summary>
        /// Access key do MinIO/S3.
        /// NUNCA colocar valor real no appsettings.json do repo.
        /// Usar variável de ambiente: ApiConfig__StorageAccessKey
        /// </summary>
        public string StorageAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Secret key do MinIO/S3.
        /// NUNCA colocar valor real no appsettings.json do repo.
        /// Usar variável de ambiente: ApiConfig__StorageSecretKey
        /// </summary>
        public string StorageSecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Se true, usa HTTPS para ligar ao MinIO. Default: false (para dev local).
        /// </summary>
        public bool StorageUseSsl { get; set; } = false;

        // --- JWT ---

        /// <summary>
        /// Chave secreta para assinar os JWT.
        /// NUNCA colocar valor real no appsettings.json do repo.
        /// Usar variável de ambiente: ApiConfig__JwtSecret
        /// </summary>
        public string JwtSecret { get; set; } = string.Empty;

        /// <summary>
        /// Emissor do JWT (Issuer). Ex: importer-api
        /// </summary>
        public string JwtIssuer { get; set; } = "importer-api";

        /// <summary>
        /// Audiência do JWT (Audience). Ex: importer-client
        /// </summary>
        public string JwtAudience { get; set; } = "importer-client";

        /// <summary>
        /// Duração do access token em minutos. Default: 60
        /// </summary>
        public int JwtAccessTokenMinutes { get; set; } = 60;

        /// <summary>
        /// Duração do refresh token em dias. Default: 30
        /// </summary>
        public int JwtRefreshTokenDays { get; set; } = 30;

        // --- Base de dados ---

        /// <summary>
        /// Connection string do PostgreSQL.
        /// NUNCA colocar valor real no appsettings.json do repo.
        /// Usar variável de ambiente: ApiConfig__DatabaseConnectionString
        /// </summary>
        public string DatabaseConnectionString { get; set; } = string.Empty;

        // --- Ambiente ---

        /// <summary>
        /// Nome do ambiente. Ex: Development, Staging, Production.
        /// </summary>
        public string EnvironmentName { get; set; } = "Development";
    }
}
