window.mwImageLoad = {
    waitForVisibleImages: function (timeoutMs) {
        return new Promise(function (resolve) {
            const imgs = Array.from(document.querySelectorAll('img'))
                .filter(img => img.getAttribute('loading') !== 'lazy');

            if (imgs.length === 0) {
                resolve(true);
                return;
            }

            let pending = imgs.length;
            let resolved = false;

            function done() {
                if (resolved) return;
                pending--;
                if (pending <= 0) {
                    resolved = true;
                    resolve(true);
                }
            }

            imgs.forEach(function (img) {
                if (img.complete && img.naturalHeight !== 0) {
                    done();
                    return;
                }
                img.addEventListener('load', done, { once: true });
                img.addEventListener('error', done, { once: true });
            });

            setTimeout(function () {
                if (!resolved) {
                    resolved = true;
                    resolve(false);
                }
            }, timeoutMs || 5000);
        });
    }
};