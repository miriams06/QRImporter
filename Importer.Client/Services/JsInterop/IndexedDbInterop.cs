using Microsoft.JSInterop;

namespace Importer.Client.Services.JsInterop
{
    /// <summary>
    /// Interop com IndexedDB via JavaScript.
    /// </summary>
    public class IndexedDbInterop
    {
        private readonly IJSRuntime _js;

        public IndexedDbInterop(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SaveAsync(object document)
        {
            // TODO [M6-G1]:
            // - Invocar indexedDbInterop.saveDocument
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<object>> GetAllAsync()
        {
            // TODO [M6-G1]:
            // - Invocar indexedDbInterop.getAllDocuments
            await Task.CompletedTask;
            return Enumerable.Empty<object>();
        }
    }
}
