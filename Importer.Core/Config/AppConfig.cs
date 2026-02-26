using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Config
{
    /// <summary>
    /// Configuração pública do Client (Blazor WASM).
    /// Vai para o browser.
    /// </summary>
    public class AppConfig
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public FeatureFlags FeatureFlags { get; set; } = new();
    }

}
