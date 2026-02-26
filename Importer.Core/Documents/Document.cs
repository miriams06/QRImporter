using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Importer.Core.QR;

namespace Importer.Core.Documents
{
    /// <summary>
    /// Representa um documento fiscal processado pela aplicação.
    /// Alinhado com o Módulo E e F.
    /// </summary>
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

        public QrData QrData { get; set; } = new();
        public string QrRawPayload { get; set; } = string.Empty;
    }
}
