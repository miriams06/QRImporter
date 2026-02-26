using Importer.Client.Models;
using Microsoft.JSInterop;

namespace Importer.Client.Services
{
    /// <summary>
    /// Serviço de leitura de QR Code ATCUD.
    /// Decode é feito 100% client-side via Web Worker.
    /// </summary>
    public class QrDecodeService
    {
        private readonly IJSRuntime _js;

        public QrDecodeService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Tenta descodificar um QR Code a partir de uma imagem/canvas.
        /// </summary>
        public async Task<string?> DecodeAsync()
        {
            // TODO [M4-D1]:
            // - Enviar imagem para Web Worker
            // - Pipeline:
            //    decode direto
            //    contraste/binarização
            //    alta resolução (PDF)
            // - Retornar payload bruto do QR
            // - Medir t_qr_decode
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Fallback manual: crop da área do QR.
        /// </summary>
        public async Task<string?> DecodeFromCropAsync()
        {
            // TODO [M4-D2]:
            // - UI permite selecionar área do QR
            // - Reprocessar apenas o crop
            // - Permitir avançar mesmo em QR difícil
            await Task.CompletedTask;
            return null;
        }
    }

}
