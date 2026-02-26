using Microsoft.JSInterop;

namespace Importer.Client.Services
{
    /// <summary>
    /// Renderiza PDFs no client-side usando pdf.js (JS Interop).
    /// </summary>
    public sealed class PdfRenderService
    {
        private readonly IJSRuntime _js;

        public PdfRenderService(IJSRuntime js) => _js = js;

        /// <summary>
        /// Renderiza uma página específica de um PDF num canvas.
        /// </summary>
        public async Task RenderPageAsync(
            byte[] pdfData,
            string canvasId,
            int pageNumber = 1,
            int dpi = 200)
        {
            if (pdfData is null || pdfData.Length == 0)
                throw new ArgumentException("PDF vazio.", nameof(pdfData));

            var base64 = Convert.ToBase64String(pdfData);

            await _js.InvokeAsync<object>(
                "pdfInterop.renderPageFromBase64",
                base64,
                canvasId,
                pageNumber,
                dpi
            );
        }
    }
}