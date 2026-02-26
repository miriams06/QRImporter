using Microsoft.AspNetCore.Components.Forms;

namespace Importer.Client.Services
{
    /// <summary>
    /// Mantém o ficheiro atual em memória (bytes + metadata).
    /// Evita usar IBrowserFile entre páginas, que pode ficar inválido.
    /// </summary>
    public sealed class DocumentStateService
    {
        public string? FileName { get; private set; }
        public string? ContentType { get; private set; }
        public byte[]? FileBytes { get; private set; }

        public string? QrText { get; private set; }

        public void SetQrText(string qr)
        {
            QrText = qr;
        }

        public void ClearQrText()
        {
            QrText = null;
        }
        public void SetFile(string fileName, string contentType, byte[] bytes)
        {
            FileName = fileName;
            ContentType = contentType;
            FileBytes = bytes;
        }

        public void Clear()
        {
            FileName = null;
            ContentType = null;
            FileBytes = null;
        }
    }
}
