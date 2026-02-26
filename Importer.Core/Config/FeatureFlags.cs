using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Config
{

    /// <summary>
    /// Flags de funcionalidade controladas por ambiente.
    /// </summary>
    public class FeatureFlags
    {
        public bool OfflineMode { get; set; }
        public bool ServerSyncEnabled { get; set; }
        public bool QrReprocessingEnabled { get; set; }
    }

}
