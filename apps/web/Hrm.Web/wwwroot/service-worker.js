// HRM System - Minimal Offline Fallback Service Worker
// Muc tieu: Chi cache duy nhat trang offline.html va tra ve khi mat ket noi mang / server ngat.
// Khong cache toan bo ung dung, khong cache du lieu nhan su, bang luong, token hay API response.

const CACHE_NAME = 'hrm-offline-v1';
const OFFLINE_URL = 'offline.html';

self.addEventListener('install', (event) => {
    event.waitUntil(
        (async () => {
            const cache = await caches.open(CACHE_NAME);
            // Tu dong giai quyet dung duong dan cache theo scope hien tai (ke ca khi chay duoi base path)
            const offlineUrl = new URL(OFFLINE_URL, self.registration.scope).href;
            await cache.add(new Request(offlineUrl, { cache: 'reload' }));
        })()
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        (async () => {
            const keys = await caches.keys();
            await Promise.all(
                keys.map((key) => {
                    if (key !== CACHE_NAME) {
                        return caches.delete(key);
                    }
                })
            );
            await self.clients.claim();
        })()
    );
});

self.addEventListener('fetch', (event) => {
    // Chi xu ly cac yeu cau dieu huong HTML (Navigation requests / F5 trang)
    if (event.request.mode === 'navigate') {
        event.respondWith(
            (async () => {
                try {
                    // Chien luoc Network-first: Luon thu ket noi toi server truoc
                    return await fetch(event.request);
                } catch (error) {
                    // Khi server tat / mat ket noi (ERR_CONNECTION_REFUSED), tra ve trang offline tu cache
                    const cache = await caches.open(CACHE_NAME);
                    const offlineUrl = new URL(OFFLINE_URL, self.registration.scope).href;
                    const cachedResponse = await cache.match(offlineUrl);
                    if (cachedResponse) {
                        return cachedResponse;
                    }
                    throw error;
                }
            })()
        );
        return;
    }

    // Tat ca cac yeu cau con lai (API, dll, wasm, static files):
    // Khong can thiep, khong cache de dam bao code dev tren localhost luon moi nhat va bao mat du lieu.
});
