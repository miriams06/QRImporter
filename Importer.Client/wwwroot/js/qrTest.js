window.qrTest = async (imgSelector) => {
    if (typeof jsQR !== "function") {
        console.error("jsQR NÃO está carregado.");
        return;
    }

    const img = document.querySelector(imgSelector);
    if (!img) { console.error("Imagem não encontrada:", imgSelector); return; }

    const canvas = document.createElement("canvas");
    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(img, 0, 0);

    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const result = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: "attemptBoth" });

    console.log("jsQR result:", result?.data ?? null);
};
