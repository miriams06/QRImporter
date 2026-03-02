importScripts("/workers/lib/zxing/index.min.js");
console.log("qrWorker loaded v20260218-2");

function rotateImageData90(img) {
    const { data, width: w, height: h } = img;
    const dst = new Uint8ClampedArray(data.length);
    for (let y = 0; y < h; y++) {
        for (let x = 0; x < w; x++) {
            const srcIdx = (y * w + x) * 4;
            const nx = h - 1 - y;
            const ny = x;
            const dstIdx = (ny * h + nx) * 4;
            dst[dstIdx] = data[srcIdx];
            dst[dstIdx + 1] = data[srcIdx + 1];
            dst[dstIdx + 2] = data[srcIdx + 2];
            dst[dstIdx + 3] = data[srcIdx + 3];
        }
    }
    return { data: dst, width: h, height: w };
}

function invertRGBA(img) {
    const out = new Uint8ClampedArray(img.data);
    for (let i = 0; i < out.length; i += 4) {
        out[i] = 255 - out[i];
        out[i + 1] = 255 - out[i + 1];
        out[i + 2] = 255 - out[i + 2];
    }
    return { data: out, width: img.width, height: img.height };
}

function decodeCore(img) {
    const w = img.width, h = img.height;
    console.log("decodeCore a tentar:", w, "x", h);
    const rgbaClamped = img.data;
    const rgba = new Uint8Array(
        rgbaClamped.buffer.slice(rgbaClamped.byteOffset, rgbaClamped.byteOffset + rgbaClamped.byteLength)
    );

    const source = new ZXing.RGBLuminanceSource(rgba, w, h);

    const hints = new Map();
    if (ZXing.DecodeHintType?.TRY_HARDER) hints.set(ZXing.DecodeHintType.TRY_HARDER, true);
    if (ZXing.DecodeHintType?.POSSIBLE_FORMATS && ZXing.BarcodeFormat?.QR_CODE) {
        hints.set(ZXing.DecodeHintType.POSSIBLE_FORMATS, [ZXing.BarcodeFormat.QR_CODE]);
    }

    const tryDecode = (binarizer, reader) => {
        const bitmap = new ZXing.BinaryBitmap(binarizer);
        if (typeof reader.setHints === "function") reader.setHints(hints);
        const result = reader.decode(bitmap);
        return typeof result.getText === "function" ? result.getText() : result.text;
    };

    try { return tryDecode(new ZXing.HybridBinarizer(source), new ZXing.MultiFormatReader()); } catch { }
    try { return tryDecode(new ZXing.GlobalHistogramBinarizer(source), new ZXing.MultiFormatReader()); } catch { }
    try { return tryDecode(new ZXing.HybridBinarizer(source), new ZXing.QRCodeReader()); } catch { }
    return tryDecode(new ZXing.GlobalHistogramBinarizer(source), new ZXing.QRCodeReader());
}

self.onmessage = async (e) => {
    //console.log("worker recebeu:", typeof e.data.bytes, e.data.bytes?.constructor?.name, e.data.bytes?.byteLength ?? e.data.bytes?.length);
    const { id, bytes, contentType } = e.data;
    const started = performance.now();
    const maxMs = 8000;
    const timeUp = () => (performance.now() - started) > maxMs;

    try {
        const ab = bytes instanceof ArrayBuffer ? bytes : bytes.buffer;

        const blob = new Blob([ab], { type: contentType || "image/png" });
        const imageBitmap = await createImageBitmap(blob);
        //console.log("imageBitmap criado:", imageBitmap.width, "x", imageBitmap.height, "contentType:", contentType);

        const small = Math.max(imageBitmap.width, imageBitmap.height) <= 420;
        const isFullDoc = Math.max(imageBitmap.width, imageBitmap.height) >= 800;

        // scales: agressivo para recorte pequeno, leve para foto
        const scales = small ? [8, 12] : isFullDoc ? [1, 2, 3] : [1, 2];

        for (const scale of scales) {
            if (timeUp()) {
                self.postMessage({ id, ok: false, error: "Timeout interno no worker (demasiadas tentativas).", originalBytes: ab, contentType });
                return;
            }

            const canvas = new OffscreenCanvas(imageBitmap.width * scale, imageBitmap.height * scale);
            const ctx = canvas.getContext("2d", { willReadFrequently: true });
            ctx.imageSmoothingEnabled = false;

            // padding / quiet-zone
            ctx.fillStyle = "#fff";
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            const pad = small ? Math.round(Math.min(canvas.width, canvas.height) * 0.12) : Math.round(Math.min(canvas.width, canvas.height) * 0.03);

            ctx.drawImage(imageBitmap, pad, pad, canvas.width - pad * 2, canvas.height - pad * 2);

            let img = ctx.getImageData(0, 0, canvas.width, canvas.height);

            // rotações: recorte tenta 4, foto tenta 1
            const rotations = small ? 4 : isFullDoc ? 2 : 1;

            let cur = img;
            for (let r = 0; r < rotations; r++) {
                if (timeUp()) break;

                try {
                    const text = decodeCore(cur);
                    if (text) { self.postMessage({ id, ok: true, text }); return; }
                } catch { }

                try {
                    const text = decodeCore(invertRGBA(cur));
                    if (text) { self.postMessage({ id, ok: true, text }); return; }
                } catch { }

                if (r < rotations - 1) cur = rotateImageData90(cur);
            }
        }

        self.postMessage({ id, ok: false, error: "Não foi possível encontrar um QR code na imagem.", originalBytes: ab, contentType });
    } catch (err) {
        console.error("ZXing worker error:", err);
        self.postMessage({ id, ok: false, error: err?.message || String(err) });
    }
};
