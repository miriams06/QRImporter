namespace Importer.Client.Models
{
    /// <summary>
    /// Modelo de documento em modo draft (offline-first).
    /// Usado no ecrã de validação.
    /// </summary>
    public class DocumentDraft
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        // Conteúdo do ficheiro (temporário, em memória)
        public byte[]? FileContent { get; set; }

        public string Status { get; set; } = "Draft";
    }
}
