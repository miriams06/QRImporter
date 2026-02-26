(function () {
    const streams = new Map(); // key: videoElementId -> MediaStream
    const canvases = new Map(); // reuse canvas per video for performance

    function getCanvas(videoId) {
        let c = canvases.get(videoId);
        if (!c) {
            c = document.createElement("canvas");
            canvases.set(videoId, c);
        }
        return c;
    }

    async function ensureJsQrLoaded() {
        if (typeof window.jsQR === "function") return window.jsQR;
        throw new Error("jsQR não está carregado. Verifica o <script src=...> no index.html.");
    }

    async function startCamera(videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video) throw new Error(`Video element não encontrado: ${videoElementId}`);

        // iOS/Safari/PWA: playsInline evita abrir fullscreen
        video.setAttribute("playsinline", "true");
        video.muted = true;
        video.autoplay = true;

        // Preferir câmera traseira
        const constraints = {
            audio: false,
            video: {
                facingMode: { ideal: "environment" },
                width: { ideal: 1280 },
                height: { ideal: 720 }
            }
        };

        const stream = await navigator.mediaDevices.getUserMedia(constraints);

        // guarda e liga ao <video>
        streams.set(videoElementId, stream);
        video.srcObject = stream;

        // aguarda começar a tocar
        await video.play();

        return true;
    }

    function stopCamera(videoElementId) {
        const stream = streams.get(videoElementId);
        streams.delete(videoElementId);

        const video = document.getElementById(videoElementId);
        if (video) {
            video.pause();
            video.srcObject = null;
        }

        if (stream) {
            for (const t of stream.getTracks()) t.stop();
        }

        return true;
    }

    async function tryDecodeFromVideo(videoElementId) {
        const jsqr = await ensureJsQrLoaded();

        const video = document.getElementById(videoElementId);
        if (!video) throw new Error(`Video element não encontrado: ${videoElementId}`);

        // ainda não temos frame?
        if (!video.videoWidth || !video.videoHeight) return null;

        const vw = video.videoWidth;
        const vh = video.videoHeight;

        // canvas reuse
        const canvas = getCanvas(videoElementId);
        canvas.width = vw;
        canvas.height = vh;

        const ctx = canvas.getContext("2d", { willReadFrequently: true });
        ctx.imageSmoothingEnabled = false;
        ctx.drawImage(video, 0, 0, vw, vh);

        // Crop central (mais prático para o utilizador: apontar o QR ao centro)
        // Tamanho ~70% do lado menor
        const side = Math.floor(Math.min(vw, vh) * 0.70);
        const cx = Math.floor((vw - side) / 2);
        const cy = Math.floor((vh - side) / 2);

        let img = ctx.getImageData(cx, cy, side, side);

        // 1) tenta direto
        let res = jsqr(img.data, img.width, img.height, { inversionAttempts: "attemptBoth" });
        if (res?.data) return res.data;

        // 2) tenta com “quiet zone” artificial (borda branca) + upscale moderado
        const scale = 3;
        const border = Math.round(side * 0.12);

        const W = (side + border * 2) * scale;
        const H = (side + border * 2) * scale;

        const c2 = document.createElement("canvas");
        c2.width = W;
        c2.height = H;

        const ctx2 = c2.getContext("2d", { willReadFrequently: true });
        ctx2.imageSmoothingEnabled = false;

        ctx2.fillStyle = "#fff";
        ctx2.fillRect(0, 0, W, H);

        // desenhar o crop no centro
        const temp = document.createElement("canvas");
        temp.width = side;
        temp.height = side;
        const tctx = temp.getContext("2d", { willReadFrequently: true });
        tctx.putImageData(img, 0, 0);

        ctx2.drawImage(temp, border * scale, border * scale, side * scale, side * scale);

        const img2 = ctx2.getImageData(0, 0, W, H);
        res = jsqr(img2.data, img2.width, img2.height, { inversionAttempts: "attemptBoth" });
        if (res?.data) return res.data;

        return null;
    }

    window.cameraInterop = {
        startCamera,
        stopCamera,
        tryDecodeFromVideo
    };
})();