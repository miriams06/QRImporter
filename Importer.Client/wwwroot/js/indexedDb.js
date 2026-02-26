window.importerIndexedDb = (() => {
    const DB_NAME = "importer-db";
    const DB_VERSION = 1;

    const STORES = {
        documents: "documents",
        files: "files",
        outbox: "outbox"
    };

    function open() {
        return new Promise((resolve, reject) => {
            const req = indexedDB.open(DB_NAME, DB_VERSION);

            req.onupgradeneeded = (e) => {
                const db = e.target.result;

                if (!db.objectStoreNames.contains(STORES.documents)) {
                    db.createObjectStore(STORES.documents, { keyPath: "id" });
                }

                // Guarda blobs por fileKey
                if (!db.objectStoreNames.contains(STORES.files)) {
                    db.createObjectStore(STORES.files, { keyPath: "fileKey" });
                }

                if (!db.objectStoreNames.contains(STORES.outbox)) {
                    const store = db.createObjectStore(STORES.outbox, { keyPath: "documentId" });
                    store.createIndex("nextAttemptAt", "nextAttemptAt", { unique: false });
                }
            };

            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    async function put(storeName, value) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, "readwrite");
            tx.objectStore(storeName).put(value);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    }

    async function get(storeName, key) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, "readonly");
            const req = tx.objectStore(storeName).get(key);
            req.onsuccess = () => resolve(req.result ?? null);
            req.onerror = () => reject(req.error);
        });
    }

    async function getAll(storeName) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, "readonly");
            const req = tx.objectStore(storeName).getAll();
            req.onsuccess = () => resolve(req.result ?? []);
            req.onerror = () => reject(req.error);
        });
    }

    async function del(storeName, key) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, "readwrite");
            tx.objectStore(storeName).delete(key);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    }

    async function listOutboxDue(nowIso) {
        const now = new Date(nowIso).getTime();
        const db = await open();

        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORES.outbox, "readonly");
            const req = tx.objectStore(STORES.outbox).getAll();
            req.onsuccess = () => {
                const all = req.result ?? [];
                const due = all.filter(x =>
                    !x.isPermanentError && new Date(x.nextAttemptAt).getTime() <= now
                );
                resolve(due);
            };
            req.onerror = () => reject(req.error);
        });
    }

    async function putFile(fileKey, fileName, contentType, arrayBuffer) {
        const blob = new Blob([arrayBuffer], { type: contentType });
        return put(STORES.files, { fileKey, fileName, contentType, blob });
    }

    async function getFileAsBase64(fileKey) {
        const file = await get(STORES.files, fileKey);
        if (!file || !file.blob) return null;

        const ab = await file.blob.arrayBuffer();
        let binary = '';
        const bytes = new Uint8Array(ab);
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }

        return {
            fileName: file.fileName,
            contentType: file.contentType,
            base64: btoa(binary)
        };
    }

    async function getFileInfo(fileKey) {
        const file = await get(STORES.files, fileKey);
        if (!file) return null;
        return { fileName: file.fileName, contentType: file.contentType };
    }

    return {
        put, get, getAll, del,
        listOutboxDue,
        putFile,
        getFileAsBase64,
        getFileInfo
    };
})();
