using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Documents
{
    public enum DocumentStatus
    {
        Draft,
        Validated,
        Approved,
        Rejected,
        Error,
        Synced
    }
}
