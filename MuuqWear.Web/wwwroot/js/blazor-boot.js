(function () {
    'use strict';

    var currentReconnectionProcess = null;
    var circuitHasBeenUp = false;
    var pageLoadedAt = Date.now();

    function hideDefaultReconnectUi() {
        var selectors = [
            '#components-reconnect-modal',
            '#components-reconnect-dialog',
            '.components-reconnect-container',
            '.components-rejoin-modal'
        ];

        selectors.forEach(function (selector) {
            document.querySelectorAll(selector).forEach(function (el) {
                el.style.display = 'none';
                el.setAttribute('aria-hidden', 'true');
            });
        });
    }

    function showConnecting() {
        hideDefaultReconnectUi();
        document.body.classList.add('mw-nav-loading');
    }

    function hideConnecting() {
        document.body.classList.remove('mw-nav-loading');
    }

    function reloadOnce(reason) {
        if (window.__mwBlazorReloadScheduled) {
            return;
        }

        try {
            var key = 'mw-blazor-reload-' + window.location.pathname;
            var count = parseInt(sessionStorage.getItem(key) || '0', 10);
            if (count >= 2) {
                console.error('[Blazor] Stopping automatic reload after repeated failures.');
                hideConnecting();
                return;
            }

            sessionStorage.setItem(key, String(count + 1));
        } catch (_) { /* ignore storage errors */ }

        window.__mwBlazorReloadScheduled = true;
        console.info('[Blazor] Reloading page:', reason || 'circuit unavailable');
        window.location.reload();
    }

    function clearReloadGuard() {
        try {
            sessionStorage.removeItem('mw-blazor-reload-' + window.location.pathname);
        } catch (_) { /* ignore storage errors */ }
    }

    function startReconnectionProcess() {
        showConnecting();

        var isCanceled = false;
        var maxRetries = circuitHasBeenUp ? 8 : 2;
        var retryDelayMs = circuitHasBeenUp ? 2000 : 400;

        (async function () {
            for (var attempt = 0; attempt < maxRetries; attempt++) {
                if (isCanceled) {
                    return;
                }

                if (attempt > 0) {
                    await new Promise(function (resolve) {
                        window.setTimeout(resolve, retryDelayMs);
                    });
                }

                if (isCanceled) {
                    return;
                }

                try {
                    var result = await Blazor.reconnect();
                    if (!result) {
                        // Server reachable but circuit was disposed (common after hard refresh).
                        reloadOnce('circuit disposed');
                        return;
                    }

                    hideConnecting();
                    return;
                } catch (_) {
                    // Server unreachable — retry.
                }
            }

            if (!circuitHasBeenUp || Date.now() - pageLoadedAt < 20000) {
                reloadOnce('initial connection failed');
                return;
            }

            hideConnecting();
        })();

        return {
            cancel: function () {
                isCanceled = true;
                hideConnecting();
            }
        };
    }

    hideDefaultReconnectUi();

    var observer = new MutationObserver(hideDefaultReconnectUi);
    observer.observe(document.documentElement, { childList: true, subtree: true });

    Blazor.start({
        circuit: {
            reconnectionHandler: {
                onConnectionDown: function () {
                    hideDefaultReconnectUi();
                    currentReconnectionProcess = currentReconnectionProcess || startReconnectionProcess();
                },
                onConnectionUp: function () {
                    circuitHasBeenUp = true;
                    clearReloadGuard();
                    currentReconnectionProcess?.cancel();
                    currentReconnectionProcess = null;
                    hideDefaultReconnectUi();
                    hideConnecting();
                    if (window.mwAdminNav) window.mwAdminNav.clearLoading();
                }
            }
        }
    }).catch(function (err) {
        console.error('[Blazor] Failed to start:', err);
        reloadOnce('start failed');
    });
})();
