const CACHE_VERSION = "qrimporter-v2";
const APP_SHELL_CACHE = `app-shell-${CACHE_VERSION}`;

const APP_SHELL_ASSETS = [
  "/",
  "/index.html",
  "/manifest.json",
  "/favicon.png",
  "/icon-192.png",
  "/css/app.css",
  "/css/bootstrap/bootstrap.min.css",
  "/js/pdfInterop.js",
  "/js/qrInterop.js",
  "/js/cameraInterop.js",
  "/js/exportHelpers.js",
  "/js/connectivity.js",
  "/js/indexedDb.js",
  "/workers/qrWorker.js",
  "/pdfjs/pdf.min.js",
  "/pdfjs/pdf.worker.min.js",
  "/workers/lib/jsqr/jsQR.min.js",
  "/workers/lib/zxing/index.min.js"
];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(APP_SHELL_CACHE).then((cache) => cache.addAll(APP_SHELL_ASSETS))
  );
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== APP_SHELL_CACHE).map((k) => caches.delete(k)))
    )
  );
  self.clients.claim();
});

self.addEventListener("fetch", (event) => {
  if (event.request.method !== "GET") return;

  const url = new URL(event.request.url);
  if (url.pathname.startsWith("/_framework/")) {
    // Evita cachear runtime/assemblies do Blazor para não prender versões antigas
    // (pode causar erros de routing após deploys/alterações de páginas).
    event.respondWith(fetch(event.request));
    return;
  }

  event.respondWith(
    caches.match(event.request).then((cached) => {
      if (cached) return cached;

      return fetch(event.request)
        .then((response) => {
          if (!response || response.status !== 200 || response.type !== "basic") {
            return response;
          }

          const requestUrl = new URL(event.request.url);
          if (requestUrl.origin === self.location.origin) {
            const copy = response.clone();
            caches.open(APP_SHELL_CACHE).then((cache) => cache.put(event.request, copy));
          }

          return response;
        })
        .catch(() => caches.match("/index.html"));
    })
  );
});
