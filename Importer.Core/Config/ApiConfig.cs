using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Config
{
    /// <summary>
    /// Configuração privada do Backend.
    /// Nunca vai para o browser.
    /// </summary>
    public class ApiConfig
    {
        public string StorageEndpoint { get; set; } = string.Empty;
        public string StorageBucket { get; set; } = string.Empty;
        // MinIO credentials (S3)
        public string StorageAccessKey { get; set; } = string.Empty;
        public string StorageSecretKey { get; set; } = string.Empty;

        public bool StorageUseSsl { get; set; } = false;
        public string EnvironmentName { get; set; } = "Development";
    }
}
