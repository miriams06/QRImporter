window.importerConnectivity = (() => {
    let started = false;
    let dotnetRef = null;

    // handlers com referência fixa (para remover corretamente)
    function onOnline() {
        if (!dotnetRef) return;
        dotnetRef.invokeMethodAsync("OnConnectivityChanged", true);
    }

    function onOffline() {
        if (!dotnetRef) return;
        dotnetRef.invokeMethodAsync("OnConnectivityChanged", false);
    }

    return {
        isOnline: () => navigator.onLine,

        // recebe dotnetRef vindo do C#
        start: (ref) => {
            if (started) return;
            started = true;
            dotnetRef = ref;

            window.addEventListener("online", onOnline);
            window.addEventListener("offline", onOffline);

            // opcional: estado inicial imediato
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync("OnConnectivityChanged", !!navigator.onLine);
            }
        },

        stop: () => {
            if (!started) return;
            started = false;

            window.removeEventListener("online", onOnline);
            window.removeEventListener("offline", onOffline);

            dotnetRef = null;
        }
    };
})();