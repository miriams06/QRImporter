using System.Text.Json;
using Importer.Client.Models;
using Microsoft.JSInterop;
namespace Importer.Client.Services
{
    /// <summary>
    /// Serviço responsável pela exportação de documentos
    /// para JSON (integração futura com WS).
    /// </summary>
    public class ExportService
    {
        private readonly IJSRuntime _js;

        public ExportService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Exporta um documento individual para ficheiro JSON.
        /// </summary>
        public async Task ExportAsync(LocalDocument document)
        {
            // TODO [M8-I1]:
            // - Suportar export em lote
            // - Garantir formato compatível com WS futuro
            // - Incluir versão do schema

            var json = JsonSerializer.Serialize(
                document,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await _js.InvokeVoidAsync(
                "exportHelpers.downloadJson",
                $"document-{document.Id}.json",
                json);
        }
    }
}
