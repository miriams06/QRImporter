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

        p.reject(new Error(msg.error || "QR decode failed"));
    };

    function guid() {
        return crypto.randomUUID ? crypto.randomUUID() : (Date.now() + "-" + Math.random());
    }

    /**
     * Blazor serializa byte[] como base64 quando passado via InvokeAsync.
     * Normaliza qualquer formato para ArrayBuffer.
     */
    function toArrayBuffer(bytes) {
        if (bytes instanceof ArrayBuffer) return bytes;

        if (bytes && bytes.buffer instanceof ArrayBuffer)
            return bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);

        // string base64 — formato que o Blazor usa ao serializar byte[]
        if (typeof bytes === "string") {
            const bin = atob(bytes);
            const ab = new ArrayBuffer(bin.length);
            const view = new Uint8Array(ab);
            for (let i = 0; i < bin.length; i++) view[i] = bin.charCodeAt(i);
            return ab;
        }

        if (Array.isArray(bytes)) {
            const ab = new ArrayBuffer(bytes.length);
            new Uint8Array(ab).set(bytes);
            return ab;
        }

        throw new Error("Formato de bytes não reconhecido: " + typeof bytes);
    }

    function loadImage(url) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve(img);
            img.onerror = reject;
            img.src = url;
        });
    }

    /**
     * jsQR no main thread — era a lógica principal original.
     * Lê documentos inteiros com upscale e binarização.
     */
    async function tryJsQr(bytes, contentType) {
        if (typeof jsQR !== "function") return null;

        const blob = new Blob([bytes], { type: contentType || "image/png" });
        const imgUrl = URL.createObjectURL(blob);

        try {
            const img = await loadImage(imgUrl);

            const srcW = img.naturalWidth || img.width;
            const srcH = img.naturalHeight || img.height;

            const isSmall = Math.max(srcW, srcH) <= 450;
            const scale = isSmall ? 10 : 3;
            const border = Math.round(Math.min(srcW, srcH) * (isSmall ? 0.30 : 0.10));

            const W = (srcW + border * 2) * scale;
            const H = (srcH + border * 2) * scale;

            const canvas = document.createElement("canvas");
            canvas.width = W;
            canvas.height = H;

            const ctx = canvas.getContext("2d", { willReadFrequently: true });
            ctx.imageSmoothingEnabled = false;
            ctx.fillStyle = "#fff";
            ctx.fillRect(0, 0, W, H);
            ctx.drawImage(img, border * scale, border * scale, srcW * scale, srcH * scale);

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
                let d = ctx.getImageData(x, y, w, h);
                let res = jsQR(d.data, d.width, d.height, { inversionAttempts: "attemptBoth" });
                if (res?.data) return res.data;

                for (const t of thresholds) {
                    const copy = new ImageData(new Uint8ClampedArray(d.data), d.width, d.height);
                    const bw = binarizeInPlace(copy, t);
                    res = jsQR(bw.data, bw.width, bw.height, { inversionAttempts: "attemptBoth" });
                    if (res?.data) return res.data;
                }
                return null;
            }

            let got = tryRect(0, 0, W, H);
            if (got) return got;

            for (const cut of [0.12, 0.18, 0.25]) {
                const y = Math.floor(H * cut);
                got = tryRect(0, y, W, H - y);
                if (got) return got;
            }

            const side = Math.floor(Math.min(W, H) * 0.90);
            got = tryRect(Math.floor((W - side) / 2), Math.floor((H - side) / 2), side, side);
            if (got) return got;

            const side2 = Math.floor(Math.min(W, H) * 0.80);
            got = tryRect(Math.floor((W - side2) / 2), Math.floor((H - side2) / 2), side2, side2);
            if (got) return got;

            return null;
        } finally {
            URL.revokeObjectURL(imgUrl);
        }
    }



    async function cropImageBytesByPercent(bytes, contentType, crop) {
        const blob = new Blob([bytes], { type: contentType || "image/png" });
        const imgUrl = URL.createObjectURL(blob);

        try {
            const img = await loadImage(imgUrl);
            const srcW = img.naturalWidth || img.width;
            const srcH = img.naturalHeight || img.height;

            const xPct = Math.max(0, Math.min(100, Number(crop?.xPct ?? 0)));
            const yPct = Math.max(0, Math.min(100, Number(crop?.yPct ?? 0)));
            const wPct = Math.max(1, Math.min(100, Number(crop?.wPct ?? 100)));
            const hPct = Math.max(1, Math.min(100, Number(crop?.hPct ?? 100)));

            const x = Math.floor(srcW * (xPct / 100));
            const y = Math.floor(srcH * (yPct / 100));
            const w = Math.max(1, Math.floor(srcW * (wPct / 100)));
            const h = Math.max(1, Math.floor(srcH * (hPct / 100)));

            const safeW = Math.min(w, srcW - x);
            const safeH = Math.min(h, srcH - y);

            if (safeW <= 0 || safeH <= 0)
                throw new Error("Crop inválido (fora da imagem).");

            const canvas = document.createElement("canvas");
            canvas.width = safeW;
            canvas.height = safeH;
            const ctx = canvas.getContext("2d", { willReadFrequently: true });
            ctx.fillStyle = "#fff";
            ctx.fillRect(0, 0, safeW, safeH);
            ctx.drawImage(img, x, y, safeW, safeH, 0, 0, safeW, safeH);

            return await new Promise((resolve, reject) => {
                canvas.toBlob(async (b) => {
                    if (!b) {
                        reject(new Error("Não foi possível gerar crop da imagem."));
                        return;
                    }
                    resolve(await b.arrayBuffer());
                }, "image/png", 1.0);
            });
        } finally {
            URL.revokeObjectURL(imgUrl);
        }
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

            worker.postMessage({ id, bytes: ab, contentType }, [ab]);
        });
    }

    window.qrInterop = {
        /**
         * Modo rápido (default):
         * 1º jsQR no main thread (comportamento original)
         * 2º worker ZXing como fallback
         */
        decodeQrFromBytes: async (bytes, contentType, timeoutMs) => {
            // jsQR primeiro — era o que funcionava antes
            const jsqrResult = await tryJsQr(toArrayBuffer(bytes), contentType);
            if (jsqrResult) return jsqrResult;

            // fallback: worker ZXing
            // reconverte bytes porque toArrayBuffer pode ter sido transferido
            return await decodeWithWorker(toArrayBuffer(bytes), contentType, timeoutMs);
        },

        /**
         * Modo agressivo (botão manual):
         * jsQR + worker ZXing
         */
        decodeQrFromBytesAggressive: async (bytes, contentType) => {
            const jsqrResult = await tryJsQr(toArrayBuffer(bytes), contentType);
            if (jsqrResult) return jsqrResult;

            return await decodeWithWorker(toArrayBuffer(bytes), contentType, 9000);
        },

        decodeQrFromBytesCrop: async (bytes, contentType, crop, timeoutMs) => {
            const src = toArrayBuffer(bytes);
            const cropped = await cropImageBytesByPercent(src, contentType, crop);

            const jsqrResult = await tryJsQr(cropped, "image/png");
            if (jsqrResult) return jsqrResult;

            return await decodeWithWorker(cropped, "image/png", timeoutMs || 9000);
        }
    };
})();