// Cria 1 worker e expõe funções globais para o Blazor chamar
(function () {
    const worker = new Worker("/workers/qrWorker.js?v=20260218-2", { type: "classic" });
    const pending = new Map();

    worker.onmessage = async (e) => {
        const msg = e.data;
        const p = pending.get(msg.id);
        if (!p) return;
        pending.delete(msg.id);

        if (msg.ok) {
            p.resolve(msg.text);
            return;
        }

        // Não fazemos fallback pesado aqui automaticamente (para não bloquear UI)
        p.reject(new Error(msg.error || "QR decode failed"));
    };

    function guid() {
        return crypto.randomUUID ? crypto.randomUUID() : (Date.now() + "-" + Math.random());
    }

    function normalizeToArrayBuffer(bytes) {
        // normaliza bytes vindos do Blazor
        let ab;
        if (bytes instanceof ArrayBuffer) ab = bytes;
        else if (bytes && bytes.buffer && typeof bytes.byteOffset === "number")
            ab = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
        else
            ab = bytes?.buffer ?? bytes;

        return ab;
    }

    // --- jsQR fallback pesado ---
    async function tryJsQrFallback(bytes, contentType) {
        if (typeof jsQR !== "function") {
            throw new Error("jsQR não está disponível.");
        }

        const blob = new Blob([bytes], { type: contentType || "image/png" });
        const imgUrl = URL.createObjectURL(blob);

        try {
            const img = await loadImage(imgUrl);

            const srcW = img.naturalWidth || img.width;
            const srcH = img.naturalHeight || img.height;

            const isSmall = Math.max(srcW, srcH) <= 450;

            // Para recortes pequenos/densos: upscale forte
            const scale = isSmall ? 10 : 3;

            // Quiet zone "artificial"
            const border = Math.round(Math.min(srcW, srcH) * (isSmall ? 0.30 : 0.10));

            const W = (srcW + border * 2) * scale;
            const H = (srcH + border * 2) * scale;

            const canvas = document.createElement("canvas");
            canvas.width = W;
            canvas.height = H;

            const ctx = canvas.getContext("2d", { willReadFrequently: true });
            ctx.imageSmoothingEnabled = false;

            // fundo branco
            ctx.fillStyle = "#fff";
            ctx.fillRect(0, 0, W, H);

            // desenhar QR ampliado com margem
            ctx.drawImage(img, border * scale, border * scale, srcW * scale, srcH * scale);

            // thresholds típicos para JPEG
            const thresholds = isSmall ? [70, 90, 110, 130, 150, 170] : [100, 130, 160];

            function binarizeInPlace(imageData, t) {
                const d = imageData.data;
                for (let i = 0; i < d.length; i += 4) {
                    const r = d[i], g = d[i + 1], b = d[i + 2];
                    const y = (0.299 * r + 0.587 * g + 0.114 * b) | 0;
                    const v = y < t ? 0 : 255;
                    d[i] = d[i + 1] = d[i + 2] = v;
                    d[i + 3] = 255;
                }
                return imageData;
            }

            function tryRect(x, y, w, h) {
                // 1) tenta direto
                let d = ctx.getImageData(x, y, w, h);
                let res = jsQR(d.data, d.width, d.height, { inversionAttempts: "attemptBoth" });
                if (res?.data) return res.data;

                // 2) tenta binarizado (vários thresholds)
                for (const t of thresholds) {
                    const copy = new ImageData(new Uint8ClampedArray(d.data), d.width, d.height);
                    const bw = binarizeInPlace(copy, t);

                    res = jsQR(bw.data, bw.width, bw.height, { inversionAttempts: "attemptBoth" });
                    if (res?.data) return res.data;
                }

                return null;
            }

            // A) full
            let got = tryRect(0, 0, W, H);
            if (got) return got;

            // B) corta topo (remove texto "ATCUD:...")
            for (const cut of [0.12, 0.18, 0.25]) {
                const y = Math.floor(H * cut);
                got = tryRect(0, y, W, H - y);
                if (got) return got;
            }

            // C) quadrado central (QR é quadrado)
            const side = Math.floor(Math.min(W, H) * 0.90);
            const cx = Math.floor((W - side) / 2);
            const cy = Math.floor((H - side) / 2);
            got = tryRect(cx, cy, side, side);
            if (got) return got;

            // D) mais apertado
            const side2 = Math.floor(Math.min(W, H) * 0.80);
            const cx2 = Math.floor((W - side2) / 2);
            const cy2 = Math.floor((H - side2) / 2);
            got = tryRect(cx2, cy2, side2, side2);
            if (got) return got;

            return null;
        } finally {
            URL.revokeObjectURL(imgUrl);
        }
    }

    function loadImage(url) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve(img);
            img.onerror = reject;
            img.src = url;
        });
    }

    function decodeWithWorker(ab, contentType, timeoutMs) {
        const ms = (typeof timeoutMs === "number" && timeoutMs > 0) ? timeoutMs : 6000;

        return new Promise((resolve, reject) => {
            const id = guid();

            const timer = setTimeout(() => {
                pending.delete(id);
                reject(new Error("Timeout a ler QR (worker demorou demasiado)."));
            }, ms);

            pending.set(id, {
                resolve: (v) => { clearTimeout(timer); resolve(v); },
                reject: (e) => { clearTimeout(timer); reject(e); }
            });

            // transfere o ArrayBuffer para o worker (rápido)
            worker.postMessage({ id, bytes: ab, contentType }, [ab]);
        });
    }

    window.qrInterop = {
        // modo rápido (default): worker ZXing primeiro (não bloqueia UI)
        decodeQrFromBytes: async (bytes, contentType, timeoutMs) => {
            const ab = normalizeToArrayBuffer(bytes);
            return await decodeWithWorker(ab, contentType, timeoutMs);
        },

        // modo lento: usa o fallback jsQR pesado
        // (chamar apenas quando o utilizador pedir)
        decodeQrFromBytesAggressive: async (bytes, contentType) => {
            const ab = normalizeToArrayBuffer(bytes);

            // tenta jsQR pesado
            const direct = await tryJsQrFallback(ab, contentType);
            if (direct) return direct;

            // se falhar, tenta worker como última tentativa
            return await decodeWithWorker(ab, contentType, 9000);
        }
    };
})();