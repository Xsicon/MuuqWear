const SHELL = 'mw-shell-v20';
const CATALOG = 'mw-catalog-v1';

const SHELL_ASSETS = [
    '/',
    '/app.css',
    '/bootstrap/bootstrap.min.css',
    '/MuuqWear.Web.styles.css',
    '/js/site.js?v=20261021b',
    '/js/firebase-auth.js?v=20261023a',
    '/js/blazor-boot.js',
    '/js/carousel.js',
    '/js/image-load.js',
    '/_framework/blazor.web.js',
    '/favicon.ico',
    '/favicon.png',
    '/apple-touch-icon.png',
    '/favicon-192.png',
    '/images/muuqwearlogo.png',
    '/lib/swiper/swiper-bundle.min.css',
    '/lib/swiper/swiper-bundle.min.js',
    '/manifest.webmanifest'
];

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(SHELL).then(function (cache) {
            return Promise.all(SHELL_ASSETS.map(function (url) {
                return fetch(new Request(url, { cache: 'reload' })).then(function (response) {
                    if (response && response.ok) return cache.put(url, response);
                }).catch(function () { });
            }));
        }).then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.filter(function (key) {
                return key !== SHELL && key !== CATALOG;
            }).map(function (key) { return caches.delete(key); }));
        }).then(function () { return self.clients.claim(); })
    );
});

self.addEventListener('fetch', function (event) {
    var request = event.request;
    if (request.method !== 'GET') return;

    var url = new URL(request.url);
    if (url.origin !== self.location.origin) return;
    if (isPassthrough(url)) return;

    if (isCatalogGet(url)) {
        event.respondWith(networkFirst(request, CATALOG));
        return;
    }

    if (request.mode === 'navigate' || isDocument(request)) {
        event.respondWith(networkFirst(request, SHELL));
        return;
    }

    if (isShellAsset(url)) {
        event.respondWith(cacheFirst(request, SHELL));
    }
});

function isPassthrough(url) {
    var path = url.pathname;
    return path.startsWith('/admin')
        || path.startsWith('/auth')
        || path.startsWith('/checkout')
        || path.startsWith('/_blazor')
        || path.startsWith('/api/Order')
        || path.startsWith('/api/Payment')
        || path.startsWith('/api/Auth')
        || path.startsWith('/api/Cart')
        || path.startsWith('/api/Wishlist');
}

function isCatalogGet(url) {
    var path = url.pathname;
    return path.startsWith('/api/Product')
        || path.startsWith('/api/Category')
        || path === '/api/Journal'
        || path.startsWith('/api/Journal/');
}

function isDocument(request) {
    return (request.headers.get('accept') || '').indexOf('text/html') !== -1;
}

function isShellAsset(url) {
    var path = url.pathname;
    return path.startsWith('/_framework/')
        || path.startsWith('/css/')
        || path.startsWith('/js/')
        || path.startsWith('/lib/')
        || path.startsWith('/images/')
        || path.endsWith('.css')
        || path.endsWith('.js')
        || path.endsWith('.wasm')
        || path.endsWith('.png')
        || path.endsWith('.woff2');
}

function networkFirst(request, cacheName) {
    return caches.open(cacheName).then(function (cache) {
        return fetch(request).then(function (response) {
            if (response && response.ok) cache.put(request, response.clone());
            return response;
        }).catch(function () {
            return cache.match(request).then(function (cached) {
                if (cached) return cached;
                if (request.mode === 'navigate') return cache.match('/');
                return Promise.reject(new Error('offline'));
            });
        });
    });
}

function cacheFirst(request, cacheName) {
    return caches.open(cacheName).then(function (cache) {
        return cache.match(request).then(function (cached) {
            if (cached) return cached;
            return fetch(request).then(function (response) {
                if (response && response.ok) cache.put(request, response.clone());
                return response;
            });
        });
    });
}
