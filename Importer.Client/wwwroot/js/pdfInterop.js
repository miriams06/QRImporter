window.pdfInterop = (() => {

    function base64ToUint8Array(base64) {
        const raw = atob(base64);
        const arr = new Uint8Array(raw.length);
        for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
        return arr;
    }

    function dpiToScale(dpi) {
        const safe = (typeof dpi === "number" && dpi > 0) ? dpi : 160;
        return safe / 96.0;
    }

    async function renderPageFromBase64(base64Pdf, canvasId, pageNumber, dpi) {
        if (!window.pdfjsLib) throw new Error("pdfjsLib não está carregado. Confirma /pdfjs/pdf.min.js.");

        const bytes = base64ToUint8Array(base64Pdf);

        const loadingTask = window.pdfjsLib.getDocument({ data: bytes });
        const pdf = await loadingTask.promise;

        const pageIndex = Math.max(1, Math.min(pageNumber || 1, pdf.numPages));
        const page = await pdf.getPage(pageIndex);

        const scale = dpiToScale(dpi);
        const viewport = page.getViewport({ scale });

        const canvas = document.getElementById(canvasId);
        if (!canvas) throw new Error(`Canvas '${canvasId}' não encontrado.`);

        const ctx = canvas.getContext("2d", { willReadFrequently: true });

        canvas.width = Math.floor(viewport.width);
        canvas.height = Math.floor(viewport.height);

        ctx.fillStyle = "#fff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        await page.render({ canvasContext: ctx, viewport }).promise;

        return { pages: pdf.numPages, renderedPage: pageIndex, width: canvas.width, height: canvas.height };
    }

    async function renderAndDecodeFromBase64(base64Pdf, canvasId, pageNumber, dpi) {
        if (!window.pdfjsLib) throw new Error("pdfjsLib não está carregado. Confirma /pdfjs/pdf.min.js.");
        if (typeof window.jsQR !== "function") throw new Error("jsQR não está carregado. Confirma /workers/lib/jsqr/jsQR.min.js.");

        const bytes = base64ToUint8Array(base64Pdf);

        const loadingTask = window.pdfjsLib.getDocument({ data: bytes });
        const pdf = await loadingTask.promise;

        const pageIndex = Math.max(1, Math.min(pageNumber || 1, pdf.numPages));
        const page = await pdf.getPage(pageIndex);

        const scale = dpiToScale(dpi);
        const viewport = page.getViewport({ scale });

        const canvas = document.getElementById(canvasId);
        if (!canvas) throw new Error(`Canvas '${canvasId}' não encontrado.`);

        const ctx = canvas.getContext("2d", { willReadFrequently: true });

        canvas.width = Math.floor(viewport.width);
        canvas.height = Math.floor(viewport.height);

        ctx.fillStyle = "#fff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        await page.render({ canvasContext: ctx, viewport }).promise;

        // decode (pode ser pesado, por isso mantemos DPI moderado do lado C#)
        const img = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const qr = window.jsQR(img.data, img.width, img.height, { inversionAttempts: "attemptBoth" });

        return {
            pages: pdf.numPages,
            renderedPage: pageIndex,
            qrText: qr?.data ?? null
        };
    }

    return { renderPageFromBase64, renderAndDecodeFromBase64 };
})();