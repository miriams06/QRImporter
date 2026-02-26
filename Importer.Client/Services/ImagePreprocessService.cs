using Microsoft.JSInterop;

namespace Importer.Client.Services
{
    /// <summary>
    /// Pré-processamento de imagens antes do decode do QR.
    /// Executado em JS para melhor performance.
    /// </summary>
    public class ImagePreprocessService
    {
        private readonly IJSRuntime _js;

        public ImagePreprocessService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Prepara a imagem para leitura do QR.
        /// </summary>
        public async Task PrepareImageAsync()
        {
            // TODO [M4-C2]:
            // - Resize da imagem (ex: largura máx. 2000px)
            // - Ajuste de contraste/binarização leve
            // - Pipeline de tentativas (normal -> otimizada)
            // - Execução em Web Worker
            await Task.CompletedTask;
        }
    }
}
