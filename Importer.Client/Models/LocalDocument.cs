using System.Text.Json;

namespace Importer.Client.Models
{
    /// <summary>
    /// Representa um documento guardado localmente (offline-first).
    /// Usado para IndexedDB, Sync e Histórico.
    /// </summary>
    public class LocalDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Origem do documento (Upload | Camera).
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento (ex: Fatura, Nota de Crédito).
        /// </summary>
        public string DocumentType { get; set; } = string.Empty;

        public string Series { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;

        public DateTime DocumentDate { get; set; }

        public string IssuerTaxId { get; set; } = string.Empty;
        public string IssuerName { get; set; } = string.Empty;
        public string IssuerAddress { get; set; } = string.Empty;

        public string ATCUD { get; set; } = string.Empty;

        /// <summary>
        /// Payload bruto do QR Code.
        /// </summary>
        public string QrRawPayload { get; set; } = string.Empty;

        /// <summary>
        /// Resultado estruturado do parse do QR (JSON).
        /// </summary>
        public JsonDocument? QrParsedData { get; set; }

        /// <summary>
        /// Estado atual de sincronização.
        /// </summary>
        public SyncStatus SyncStatus { get; set; } = SyncStatus.Draft;

        /// <summary>
        /// Caminho/identificador do ficheiro no IndexedDB.
        /// </summary>
        public string FileKey { get; set; } = string.Empty;

        /// <summary>
        /// Última mensagem de erro (se aplicável).
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
