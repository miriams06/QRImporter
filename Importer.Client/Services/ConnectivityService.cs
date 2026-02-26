using Microsoft.JSInterop;

namespace Importer.Client.Services
{
    public sealed class ConnectivityService
    {
        private readonly IJSRuntime _js;
        private DotNetObjectReference<ConnectivityService>? _ref;

        public bool LastKnownOnline { get; private set; } = true;

        public event Func<bool, Task>? OnlineChanged;

        public ConnectivityService(IJSRuntime js) => _js = js;

        public async Task StartAsync()
        {
            if (_ref is not null) return;

            _ref = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("importerConnectivity.start", _ref);
        }

        public async Task<bool> IsOnlineAsync()
            => await _js.InvokeAsync<bool>("importerConnectivity.isOnline");

        [JSInvokable]
        public async Task OnConnectivityChanged(bool isOnline)
        {
            LastKnownOnline = isOnline;
            if (OnlineChanged is not null)
                await OnlineChanged.Invoke(isOnline);
        }

        public async ValueTask DisposeAsync()
        {
            try { await _js.InvokeVoidAsync("importerConnectivity.stop"); } catch { }
            _ref?.Dispose();
            _ref = null;
        }
    }
}
