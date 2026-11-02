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
    },

    markReady: function (img) {
        if (!img || img.tagName !== 'IMG' || img.classList.contains('mw-img-ready')) {
            return;
        }
        img.classList.add('mw-img-ready');
    },

    scan: function (root) {
        var scope = root || document;
        var imgs = scope.querySelectorAll
            ? scope.querySelectorAll('img')
            : [];
        Array.prototype.forEach.call(imgs, function (img) {
            if (img.complete) {
                window.mwImageLoad.markReady(img);
            }
        });
    },

    watch: function () {
        if (window.__mwImgWatch) {
            window.mwImageLoad.scan();
            return;
        }
        window.__mwImgWatch = true;

        document.addEventListener('load', function (e) {
            var t = e.target;
            if (t && t.tagName === 'IMG') {
                window.mwImageLoad.markReady(t);
            }
        }, true);

        document.addEventListener('error', function (e) {
            var t = e.target;
            if (t && t.tagName === 'IMG') {
                window.mwImageLoad.markReady(t);
            }
        }, true);

        if (window.MutationObserver && document.body) {
            var mo = new MutationObserver(function (mutations) {
                for (var i = 0; i < mutations.length; i++) {
                    var nodes = mutations[i].addedNodes;
                    for (var j = 0; j < nodes.length; j++) {
                        var node = nodes[j];
                        if (node.nodeType !== 1) continue;
                        if (node.tagName === 'IMG') {
                            if (node.complete) window.mwImageLoad.markReady(node);
                        } else if (node.querySelectorAll) {
                            window.mwImageLoad.scan(node);
                        }
                    }
                }
            });
            mo.observe(document.body, { childList: true, subtree: true });
        }

        window.mwImageLoad.scan();
    }
};

window.mwLazySections = {
    observe: function (dotNetRef, rootMargin) {
        var nodes = document.querySelectorAll('[data-mw-lazy-section]');
        if (!nodes.length) {
            return;
        }

        function notify(id) {
            if (!id || !dotNetRef) return;
            dotNetRef.invokeMethodAsync('OnSectionVisible', id).catch(function () { });
        }

        if (!window.IntersectionObserver) {
            Array.prototype.forEach.call(nodes, function (el) {
                notify(el.getAttribute('data-mw-lazy-section'));
            });
            return;
        }

        if (window.__mwLazySectionsIo) {
            window.__mwLazySectionsIo.disconnect();
            window.__mwLazySectionsIo = null;
        }

        var seen = {};
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                var id = entry.target.getAttribute('data-mw-lazy-section');
                if (!id || seen[id]) return;
                seen[id] = true;
                io.unobserve(entry.target);
                notify(id);
            });
        }, {
            rootMargin: rootMargin || '200px 0px',
            threshold: 0.01
        });

        Array.prototype.forEach.call(nodes, function (el) {
            io.observe(el);
        });
        window.__mwLazySectionsIo = io;
    },

    dispose: function () {
        if (window.__mwLazySectionsIo) {
            window.__mwLazySectionsIo.disconnect();
            window.__mwLazySectionsIo = null;
        }
    }
};
