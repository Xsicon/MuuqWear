window.focusElement = (element) => {
    if (element) {
        element.focus();
    }
};

window.mwInitHeroCarousel = function (autoplayMs) {
    var autoplay = autoplayMs || 5500;

    function init() {
        if (typeof Swiper === "undefined") {
            setTimeout(init, 50);
            return;
        }

        var heroEl = document.querySelector(".mw-hero");
        if (!heroEl) return;

        if (heroEl.swiper) {
            heroEl.swiper.autoplay?.start();
            return;
        }

        var section = heroEl.closest(".mw-hero-section");
        if (!section) return;

        var segments = section.querySelectorAll(".mw-hero-progress__segment");
        var prevBtn = section.querySelector(".mw-hero-nav--prev");
        var nextBtn = section.querySelector(".mw-hero-nav--next");

        section.style.setProperty("--hero-autoplay-ms", autoplay + "ms");

        function updateProgress(realIndex) {
            segments.forEach(function (seg, i) {
                seg.classList.remove("is-active", "is-complete");
                var fill = seg.querySelector(".mw-hero-progress__fill");
                if (fill) {
                    fill.style.animation = "none";
                    void fill.offsetWidth;
                    fill.style.animation = "";
                }
                if (i < realIndex) seg.classList.add("is-complete");
                if (i === realIndex) seg.classList.add("is-active");
            });
        }

        var swiper = new Swiper(heroEl, {
            loop: true,
            effect: "fade",
            fadeEffect: { crossFade: true },
            speed: 900,
            autoplay: {
                delay: autoplay,
                disableOnInteraction: false,
                pauseOnMouseEnter: false
            },
            navigation: {
                nextEl: nextBtn,
                prevEl: prevBtn
            },
            on: {
                init: function (s) {
                    updateProgress(s.realIndex);
                },
                slideChange: function (s) {
                    updateProgress(s.realIndex);
                }
            }
        });

        swiper.autoplay.start();
    }

    init();
};

window.mwDestroyHeroCarousel = function () {
    var heroEl = document.querySelector(".mw-hero");
    if (heroEl && heroEl.swiper) {
        heroEl.swiper.destroy(true, true);
    }
};

window.mwInitHomeCarousels = function () {
    if (typeof Swiper === "undefined") {
        setTimeout(window.mwInitHomeCarousels, 50);
        return;
    }

    var presets = {
        category: {
            slidesPerView: 1,
            spaceBetween: 16,
            breakpoints: {
                768: { slidesPerView: 2, spaceBetween: 26 },
                1024: { slidesPerView: 3, spaceBetween: 26 }
            }
        },
        product: {
            slidesPerView: 1,
            spaceBetween: 16,
            breakpoints: {
                768: { slidesPerView: 2.2, spaceBetween: 26 },
                1024: { slidesPerView: 4, spaceBetween: 26 }
            }
        },
        collection: {
            slidesPerView: 1,
            spaceBetween: 16,
            breakpoints: {
                768: { slidesPerView: 1.5, spaceBetween: 26 },
                1024: { slidesPerView: 2, spaceBetween: 26 }
            }
        }
    };

    document.querySelectorAll(".mw-carousel-section").forEach(function (section) {
        var swiperEl = section.querySelector(".mw-home-swiper");
        if (!swiperEl) return;

        if (swiperEl.swiper) {
            mwUnbindHomeNav(section);
            swiperEl.swiper.destroy(true, true);
        }

        var type = section.getAttribute("data-carousel") || "product";
        var preset = presets[type] || presets.product;
        var prevBtn = section.querySelector(".mw-carousel-section__prev");
        var nextBtn = section.querySelector(".mw-carousel-section__next");
        var slideCount = swiperEl.querySelectorAll(".swiper-slide").length;
        var canWrap = slideCount > 1;

        var swiper = new Swiper(swiperEl, {
            slidesPerView: preset.slidesPerView,
            spaceBetween: preset.spaceBetween,
            breakpoints: preset.breakpoints,
            slidesPerGroup: 1,
            speed: 600,
            grabCursor: true,
            touchRatio: 1,
            watchOverflow: slideCount <= 1,
            observer: true,
            observeParents: true,
            resizeObserver: true
        });

        requestAnimationFrame(function () {
            if (!swiper.destroyed) swiper.update();
        });

        if (canWrap) {
            mwBindHomeNavWrap(swiper, prevBtn, nextBtn, slideCount);
        }
    });
};

function mwBindHomeNavWrap(swiper, prevBtn, nextBtn, slideCount) {
    function onPrevClick() {
        if (swiper.destroyed || swiper.animating) return;
        if (swiper.isBeginning) swiper.slideTo(slideCount - 1, swiper.params.speed);
        else swiper.slidePrev();
    }

    function onNextClick() {
        if (swiper.destroyed || swiper.animating) return;
        if (swiper.isEnd) swiper.slideTo(0, swiper.params.speed);
        else swiper.slideNext();
    }

    if (prevBtn) {
        prevBtn._mwNavHandler = onPrevClick;
        prevBtn.addEventListener("click", onPrevClick);
    }

    if (nextBtn) {
        nextBtn._mwNavHandler = onNextClick;
        nextBtn.addEventListener("click", onNextClick);
    }
}

function mwUnbindHomeNav(section) {
    section.querySelectorAll(".mw-carousel-section__prev, .mw-carousel-section__next").forEach(function (btn) {
        if (btn._mwNavHandler) {
            btn.removeEventListener("click", btn._mwNavHandler);
            delete btn._mwNavHandler;
        }
    });
}

window.mwDestroyHomeCarousels = function () {
    document.querySelectorAll(".mw-carousel-section").forEach(function (section) {
        mwUnbindHomeNav(section);
    });

    document.querySelectorAll(".mw-home-swiper").forEach(function (el) {
        if (el.swiper) {
            el.swiper.destroy(true, true);
        }
    });
};

window.disableScroll = () => {
    document.body.classList.add("no-scroll");
};

window.enableScroll = () => {
    document.body.classList.remove("no-scroll");
};

window.submitAuthForm = function (html) {
    try {
        document.open('text/html', 'replace');
        document.write(html);
        document.close();
    } catch (e) {
        // fallback
        var div = document.createElement('div');
        div.innerHTML = html;
        document.body.appendChild(div);
        var form = div.querySelector('form');
        if (form) form.submit();
    }
};

window.initMagnifier = function (imgSelector) {
    const img = document.querySelector(imgSelector);
    if (!img) return;

    // create lens element
    const lens = document.createElement('div');
    lens.classList.add('mw-magnifier-lens');
    img.parentElement.style.position = 'relative';
    img.parentElement.appendChild(lens);

    const ZOOM = 3;      // 3x zoom
    const LENS_SIZE = 150; // 150px lens

    lens.style.width = LENS_SIZE + 'px';
    lens.style.height = LENS_SIZE + 'px';

    function moveLens(e) {
        e.preventDefault();

        // get cursor position relative to image
        const rect = img.getBoundingClientRect();
        let x = e.clientX - rect.left;
        let y = e.clientY - rect.top;

        // keep lens inside image bounds
        const halfLens = LENS_SIZE / 2;
        x = Math.max(halfLens, Math.min(x, rect.width - halfLens));
        y = Math.max(halfLens, Math.min(y, rect.height - halfLens));

        // position lens centered on cursor
        lens.style.left = (x - halfLens) + 'px';
        lens.style.top = (y - halfLens) + 'px';

        // calculate background position for zoom effect
        // background shows zoomed portion of image
        const bgX = ((x / rect.width) * 100);
        const bgY = ((y / rect.height) * 100);

        lens.style.backgroundImage = `url('${img.src}')`;
        lens.style.backgroundSize = `${rect.width * ZOOM}px ${rect.height * ZOOM}px`;
        lens.style.backgroundPosition = `${bgX}% ${bgY}%`;
    }

    // show lens on mouse enter
    img.addEventListener('mouseenter', () => {
        lens.style.display = 'block';
    });

    // move lens with cursor
    img.addEventListener('mousemove', moveLens);

    // hide lens on mouse leave
    img.addEventListener('mouseleave', () => {
        lens.style.display = 'none';
    });
};

window.destroyMagnifier = function (imgSelector) {
    const img = document.querySelector(imgSelector);
    if (!img) return;

    // remove lens if exists
    const lens = img.parentElement.querySelector('.mw-magnifier-lens');
    if (lens) lens.remove();
};

// Guest cart and wishlist live in IndexedDB. Existing cookies are copied once.
window.mwBag = {
    _db: null,
    open: function () {
        var self = this;
        if (self._db) return Promise.resolve(self._db);
        return new Promise(function (resolve, reject) {
            var request = indexedDB.open('mw-store', 2);
            request.onupgradeneeded = function () {
                var db = request.result;
                if (!db.objectStoreNames.contains('bags'))
                    db.createObjectStore('bags');
                if (!db.objectStoreNames.contains('catalog'))
                    db.createObjectStore('catalog');
                if (!db.objectStoreNames.contains('outbox'))
                    db.createObjectStore('outbox', { keyPath: 'id' });
            };
            request.onsuccess = function () {
                self._db = request.result;
                resolve(self._db);
            };
            request.onerror = function () { reject(request.error); };
        });
    },
    get: function (key) {
        return this.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                var request = db.transaction('bags', 'readonly').objectStore('bags').get(key);
                request.onsuccess = function () { resolve(request.result || null); };
                request.onerror = function () { reject(request.error); };
            });
        });
    },
    set: function (key, value) {
        return this.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                var request = db.transaction('bags', 'readwrite').objectStore('bags').put(value, key);
                request.onsuccess = function () { resolve(); };
                request.onerror = function () { reject(request.error); };
            });
        });
    },
    remove: function (key) {
        return this.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                var request = db.transaction('bags', 'readwrite').objectStore('bags').delete(key);
                request.onsuccess = function () { resolve(); };
                request.onerror = function () { reject(request.error); };
            });
        });
    }
};

function mwReadCookie(name) {
    var cookies = document.cookie.split(';');
    var prefix = name + '=';
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].trim();
        if (cookie.indexOf(prefix) === 0)
            return decodeURIComponent(cookie.substring(prefix.length));
    }
    return null;
}

function mwExpireCookie(name) {
    document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Lax';
}

function mwLoadBag(key, cookieName) {
    return window.mwBag.get(key).then(function (stored) {
        if (stored) return stored;
        var legacy = mwReadCookie(cookieName);
        if (!legacy) return null;
        return window.mwBag.set(key, legacy).then(function () {
            mwExpireCookie(cookieName);
            return legacy;
        });
    }).catch(function (e) {
        console.error('mwLoadBag error:', e);
        return mwReadCookie(cookieName);
    });
}

function mwSaveBag(key, cookieName, json) {
    return window.mwBag.set(key, json).then(function () {
        mwExpireCookie(cookieName);
    }).catch(function (e) {
        console.error('mwSaveBag error:', e);
    });
}

function mwClearBag(key, cookieName) {
    return window.mwBag.remove(key).then(function () {
        mwExpireCookie(cookieName);
    }).catch(function (e) {
        console.error('mwClearBag error:', e);
        mwExpireCookie(cookieName);
    });
}

window.getCartCookie = function () { return mwLoadBag('cart', 'muuqwear_cart'); };
window.setCartCookie = function (json) { return mwSaveBag('cart', 'muuqwear_cart', json); };
window.clearCartCookie = function () { return mwClearBag('cart', 'muuqwear_cart'); };
window.getWishlistCookie = function () { return mwLoadBag('wishlist', 'muuqwear_wishlist'); };
window.setWishlistCookie = function (json) { return mwSaveBag('wishlist', 'muuqwear_wishlist', json); };
window.clearWishlistCookie = function () { return mwClearBag('wishlist', 'muuqwear_wishlist'); };

// Sign in via single POST — no cache key round-trip (avoids json=1 fetch race).
window.mwSignIn = function (session) {
    return fetch("/auth/sign-in", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json", "X-Requested-With": "MuuqWear" },
        body: JSON.stringify(session)
    })
        .then(function (res) { return res.ok; })
        .catch(function (e) {
            console.error("mwSignIn error:", e);
            return false;
        });
};

window.mwCompleteStaffSignIn = function (ticket, returnUrl) {
    var form = document.createElement("form");
    form.method = "POST";
    form.action = "/auth/complete";
    form.style.display = "none";
    function field(name, value) {
        var input = document.createElement("input");
        input.type = "hidden";
        input.name = name;
        input.value = value || "";
        form.appendChild(input);
    }
    field("ticket", ticket);
    field("returnUrl", returnUrl || "/admin");
    document.body.appendChild(form);
    form.submit();
    return true;
};

// Legacy cache-key path (fallback only).
window.mwSetCookieFromKey = function (key) {
    return fetch(
        "/auth/set-cookie?key=" + encodeURIComponent(key) + "&json=1",
        { method: "GET", credentials: "include" }
    )
        .then(function (res) { return res.ok; })
        .catch(function (e) {
            console.error("mwSetCookieFromKey error:", e);
            return false;
        });
};

window.mwAuth = {
    signIn: function (session) {
        return window.mwSignIn(session);
    },
    setCookieFromKey: function (key) {
        return window.mwSetCookieFromKey(key);
    }
};

window.mwScrollLock = {
    lock: function () {
        document.body.classList.add("mw-spinner-active");
    },
    unlock: function () {
        document.body.classList.remove("mw-spinner-active");
    }
};

window.mwNavProgress = {
    _initialized: false,
    _awaitingPageReady: false,
    _staticFallback: null,

    start: function () {
        window.mwNavProgress._awaitingPageReady = true;
        window.mwNavProgress._startedAt = Date.now();
        document.body.classList.add("mw-nav-loading");
        clearTimeout(window.mwNavProgress._staticFallback);
    },

    pageReady: function () {
        if (!window.mwNavProgress._awaitingPageReady) {
            return;
        }

        window.mwNavProgress._awaitingPageReady = false;
        clearTimeout(window.mwNavProgress._staticFallback);

        if (window.mwAdminNav) {
            window.mwAdminNav.clearLoading();
        }

        if (window.mwStorefrontNav) {
            window.mwStorefrontNav.clearLoading();
        }

        window.mwNavProgress.done();
    },

    done: function () {
        var elapsed = Date.now() - (window.mwNavProgress._startedAt || 0);
        var remaining = Math.max(0, 150 - elapsed);

        window.setTimeout(function () {
            document.body.classList.remove("mw-nav-loading");
        }, remaining);
    },

    onEnhancedLoad: function () {
        var self = window.mwNavProgress;
        clearTimeout(self._staticFallback);

        // Static pages that never signal readiness auto-complete quickly.
        self._staticFallback = window.setTimeout(function () {
            if (self._awaitingPageReady) {
                self.pageReady();
            }
        }, 400);
    },

    init: function () {
        if (window.mwNavProgress._initialized) return;
        window.mwNavProgress._initialized = true;

        document.addEventListener("click", function (e) {
            var link = e.target.closest("a[href]");
            var navBtn = e.target.closest(".an-nav__subitem, .an-nav__item[href]");

            if (!link && !navBtn) return;

            var el = link || navBtn;
            if (el.target === "_blank" || el.hasAttribute("download")) return;

            var href = el.getAttribute("href");
            if (!href || href.charAt(0) === "#") return;
            if (href.indexOf("javascript:") === 0) return;

            if (href.indexOf("http") === 0 || href.indexOf("//") === 0) {
                try {
                    var url = new URL(href);
                    if (url.origin !== window.location.origin) return;
                    href = url.pathname + url.search + url.hash;
                } catch (_) {
                    return;
                }
            }

            var current = window.location.pathname + window.location.search;
            if (href === current || href === current + window.location.hash) return;

            window.mwNavProgress.start();
        }, true);

        window.mwNavProgress._bindNavigationEnd();
    },

    _bindNavigationEnd: function () {
        if (window.Blazor) {
            Blazor.addEventListener("enhancedload", function () {
                window.mwNavProgress.onEnhancedLoad();
            });
        }

        window.addEventListener("pageshow", function () {
            window.mwNavProgress.pageReady();
        });
    }
};

window.mwStorefrontNav = {
    _initialized: false,
    _loadingEl: null,

    init: function () {
        if (this._initialized) return;
        this._initialized = true;

        var self = this;

        document.addEventListener("click", function (e) {
            var link = e.target.closest(".mw-nav__link, .mw-mobile-nav__link");
            if (!link) return;

            if (link.target === "_blank" || link.hasAttribute("download")) return;

            var href = link.getAttribute("href");
            if (!href || href.charAt(0) === "#") return;

            if (href.indexOf("http") === 0 || href.indexOf("//") === 0) {
                try {
                    var url = new URL(href);
                    if (url.origin !== window.location.origin) return;
                    href = url.pathname + url.search + url.hash;
                } catch (_) {
                    return;
                }
            }

            var current = window.location.pathname + window.location.search;
            if (href === current || href === current + window.location.hash) return;

            self.setLoading(link);
            if (window.mwNavProgress) window.mwNavProgress.start();
        }, true);
    },

    setLoading: function (el) {
        this.clearLoading();
        el.classList.add("mw-nav--loading");
        this._loadingEl = el;
    },

    clearLoading: function () {
        if (this._loadingEl) {
            this._loadingEl.classList.remove("mw-nav--loading");
            this._loadingEl = null;
        }

        document.querySelectorAll(".mw-nav--loading").forEach(function (el) {
            el.classList.remove("mw-nav--loading");
        });
    }
};

window.mwMuuqsimoCountdown = {
    timer: null,

    start: function (targetIso) {
        // Clear any prior timer (handles re-mounts during dev)
        if (this.timer) clearInterval(this.timer);

        var target = new Date(targetIso).getTime();
        if (isNaN(target)) {
            console.error('[Countdown] Invalid target date:', targetIso);
            return;
        }

        var self = this;

        function update() {
            var now = Date.now();
            var diff = target - now;

            // Event passed — show all zeros and stop
            if (diff <= 0) {
                self.render(0, 0, 0, 0);
                if (self.timer) {
                    clearInterval(self.timer);
                    self.timer = null;
                }
                return;
            }

            var days = Math.floor(diff / (1000 * 60 * 60 * 24));
            var hours = Math.floor((diff / (1000 * 60 * 60)) % 24);
            var minutes = Math.floor((diff / (1000 * 60)) % 60);
            var seconds = Math.floor((diff / 1000) % 60);

            self.render(days, hours, minutes, seconds);
        }

        function pad(n) {
            return n < 10 ? '0' + n : '' + n;
        }

        this.render = function (d, h, m, s) {
            var setText = function (key, val) {
                var el = document.querySelector('[data-ms-cd="' + key + '"]');
                if (el) el.textContent = pad(val);
            };
            setText('days', d);
            setText('hours', h);
            setText('minutes', m);
            setText('seconds', s);
        };

        update();   // initial render — don't wait 1s for first display
        this.timer = setInterval(update, 1000);
    },

    stop: function () {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
    }
};

window.mwMuuqsimoHero = {
    instance: null,

    init: function () {
        var self = this;

        function boot() {
            if (typeof Swiper === 'undefined') {
                setTimeout(boot, 50);
                return;
            }

            var el = document.querySelector('.ms-hero__carousel');
            if (!el) return;

            if (self.instance) {
                self.instance.destroy(true, true);
                self.instance = null;
            }

            self.instance = new Swiper('.ms-hero__carousel', {
                loop: true,
                effect: 'fade',
                fadeEffect: { crossFade: true },
                autoplay: { delay: 5000, disableOnInteraction: false },
                pagination: { el: '.ms-hero__pagination', clickable: true }
            });
        }

        boot();
    }
};








// MuuqWear Analytics Chart — Chart.js wrapper

window.muuqAnalyticsChart = {
    // Holds the current chart instance so we can destroy/replace it
    _chart: null,

    render: function (canvasId, labels, values) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.warn("[muuqAnalyticsChart] Canvas not found:", canvasId);
            return;
        }

        // Destroy existing chart if any (prevents overlap on refresh)
        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }

        const ctx = canvas.getContext("2d");

        // Gradient under the line
        const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height || 280);
        gradient.addColorStop(0, "rgba(30, 42, 71, 0.18)");
        gradient.addColorStop(1, "rgba(30, 42, 71, 0.0)");

        this._chart = new Chart(ctx, {
            type: "line",
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    borderColor: "#1E2A47",
                    backgroundColor: gradient,
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 0,
                    pointHoverRadius: 5,
                    pointHoverBackgroundColor: "#1E2A47",
                    pointHoverBorderColor: "#FFFFFF",
                    pointHoverBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: "#1E2A47",
                        titleColor: "#FFFFFF",
                        bodyColor: "#FFFFFF",
                        borderColor: "#1E2A47",
                        borderWidth: 0,
                        padding: 10,
                        displayColors: false,
                        callbacks: {
                            label: function (ctx) {
                                return "$" + ctx.parsed.y.toLocaleString();
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: {
                            color: "#A9B7CC",
                            font: { size: 11 },
                            maxRotation: 0,
                            autoSkip: true,
                            maxTicksLimit: 7
                        }
                    },
                    y: {
                        beginAtZero: false,
                        grid: {
                            color: "rgba(169, 183, 204, 0.2)",
                            drawBorder: false
                        },
                        ticks: {
                            color: "#A9B7CC",
                            font: { size: 11 },
                            callback: function (value) {
                                if (value >= 1000) {
                                    return "$" + (value / 1000).toFixed(0) + "k";
                                }
                                return "$" + value;
                            }
                        }
                    }
                },
                interaction: {
                    intersect: false,
                    mode: "index"
                }
            }
        });
    },

    destroy: function () {
        if (this._chart) {
            this._chart.destroy();
            this._chart = null;
        }
    }
};

window.mwCopyToClipboard = async function (text) {
    try {
        if (navigator.clipboard?.writeText) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch (_) { /* fallback below */ }

    try {
        const ta = document.createElement("textarea");
        ta.value = text;
        ta.setAttribute("readonly", "");
        ta.style.position = "absolute";
        ta.style.left = "-9999px";
        document.body.appendChild(ta);
        ta.select();
        const ok = document.execCommand("copy");
        document.body.removeChild(ta);
        return ok;
    } catch (_) {
        return false;
    }
};

window.mwLoadScripts = {
    ensure: function (src) {
        return new Promise(function (resolve, reject) {
            if (document.querySelector('script[src="' + src + '"]')) {
                resolve();
                return;
            }
            var script = document.createElement('script');
            script.src = src;
            script.onload = function () { resolve(); };
            script.onerror = function () { reject(new Error('Failed to load ' + src)); };
            document.body.appendChild(script);
        });
    },
    ensureMany: function (sources) {
        var list = sources || [];
        return Promise.all(list.map(function (src) { return window.mwLoadScripts.ensure(src); }));
    },
    ensureStripe: function () {
        return window.mwLoadScripts.ensureMany([
            'https://js.stripe.com/v3/',
            'js/stripe-interop.js'
        ]);
    },
    ensureCharts: function () {
        return window.mwLoadScripts.ensureMany([
            'https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js',
            'js/chartHelpers.js'
        ]);
    }
};

window.mwRegisterStorefrontBridge = function (ref) {
    window.__mwStorefrontBridge = ref;
};

window.mwStorefrontAddToCart = async function (request) {
    var bridge = window.__mwStorefrontBridge;
    if (!bridge) return null;
    return await bridge.invokeMethodAsync('AddToCartFromPage', request);
};

window.mwStorefrontGetCart = async function () {
    var bridge = window.__mwStorefrontBridge;
    if (!bridge) return null;
    return await bridge.invokeMethodAsync('GetCartForPage');
};

window.mwStorefrontUpdateCartQty = async function (id, qty) {
    var bridge = window.__mwStorefrontBridge;
    if (!bridge) return null;
    return await bridge.invokeMethodAsync('UpdateCartQtyFromPage', id, qty);
};

window.mwStorefrontRemoveFromCart = async function (id) {
    var bridge = window.__mwStorefrontBridge;
    if (!bridge) return null;
    return await bridge.invokeMethodAsync('RemoveCartItemFromPage', id);
};

window.mwPublishCart = function () {
    window.dispatchEvent(new CustomEvent('mw-cart-updated'));
};

window.mwWatchCart = function (id, dotNetRef) {
    var key = String(id);
    window.__mwCartHandlers = window.__mwCartHandlers || {};
    if (window.__mwCartHandlers[key])
        window.removeEventListener('mw-cart-updated', window.__mwCartHandlers[key]);
    var handler = function () {
        dotNetRef.invokeMethodAsync('OnCartUpdated');
    };
    window.__mwCartHandlers[key] = handler;
    window.addEventListener('mw-cart-updated', handler);
};

window.mwUnwatchCart = function (id) {
    var key = String(id);
    var handler = window.__mwCartHandlers && window.__mwCartHandlers[key];
    if (!handler) return;
    window.removeEventListener('mw-cart-updated', handler);
    delete window.__mwCartHandlers[key];
};

window.mwPublishWishlist = function (ids) {
    window.__mwWishlistIds = ids || [];
    window.dispatchEvent(new CustomEvent('mw-wishlist-updated'));
};

window.mwTryWishlistToggle = async function (item) {
    var bridge = window.__mwStorefrontBridge;
    if (!bridge) return -1;
    var active = await bridge.invokeMethodAsync('ToggleWishlist', item);
    return active ? 1 : 0;
};

window.mwWatchWishlist = function (id, dotNetRef) {
    var key = String(id);
    window.__mwWlHandlers = window.__mwWlHandlers || {};
    if (window.__mwWlHandlers[key])
        window.removeEventListener('mw-wishlist-updated', window.__mwWlHandlers[key]);
    var handler = function () {
        dotNetRef.invokeMethodAsync('OnWishlistUpdated', window.__mwWishlistIds || []);
    };
    window.__mwWlHandlers[key] = handler;
    window.addEventListener('mw-wishlist-updated', handler);
    handler();
};

window.mwUnwatchWishlist = function (id) {
    var key = String(id);
    var handler = window.__mwWlHandlers && window.__mwWlHandlers[key];
    if (!handler) return;
    window.removeEventListener('mw-wishlist-updated', handler);
    delete window.__mwWlHandlers[key];
};

window.mwIsOnline = function () { return navigator.onLine; };

window.mwWatchOnline = function (id, dotNetRef) {
    var key = String(id);
    window.__mwOnlineHandlers = window.__mwOnlineHandlers || {};
    if (window.__mwOnlineHandlers[key])
    {
        window.removeEventListener('online', window.__mwOnlineHandlers[key]);
        window.removeEventListener('offline', window.__mwOnlineHandlers[key]);
    }
    var handler = function () {
        dotNetRef.invokeMethodAsync('OnConnectivityChanged', navigator.onLine);
    };
    window.__mwOnlineHandlers[key] = handler;
    window.addEventListener('online', handler);
    window.addEventListener('offline', handler);
};

window.mwUnwatchOnline = function (id) {
    var key = String(id);
    var handler = window.__mwOnlineHandlers && window.__mwOnlineHandlers[key];
    if (!handler) return;
    window.removeEventListener('online', handler);
    window.removeEventListener('offline', handler);
    delete window.__mwOnlineHandlers[key];
};

window.mwTokenStore = {
    key: 'mw-refresh',
    getRefresh: function () {
        try { return sessionStorage.getItem(this.key); } catch (e) { return null; }
    },
    setRefresh: function (value) {
        try {
            if (value) sessionStorage.setItem(this.key, value);
            else sessionStorage.removeItem(this.key);
        } catch (e) { }
    }
};

window.mwCatalogCache = {
    maxChars: 400000,
    get: function (key) {
        return window.mwBag.open().then(function (db) {
            return new Promise(function (resolve) {
                if (!db.objectStoreNames.contains('catalog')) {
                    resolve(null);
                    return;
                }
                var request = db.transaction('catalog', 'readonly').objectStore('catalog').get(key);
                request.onsuccess = function () { resolve(request.result || null); };
                request.onerror = function () { resolve(null); };
            });
        }).catch(function () { return null; });
    },
    set: function (key, body, contentType) {
        if (!body || body.length > this.maxChars) return Promise.resolve();
        var payload = {
            savedAt: Date.now(),
            body: body,
            contentType: contentType || 'application/json'
        };
        return window.mwBag.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                if (!db.objectStoreNames.contains('catalog')) {
                    resolve();
                    return;
                }
                var request = db.transaction('catalog', 'readwrite').objectStore('catalog').put(payload, key);
                request.onsuccess = function () { resolve(); };
                request.onerror = function () { reject(request.error); };
            });
        }).catch(function () { });
    }
};

window.mwOutbox = {
    enqueue: function (kind, payload) {
        var item = {
            id: Date.now() + '-' + Math.random().toString(16).slice(2),
            kind: kind,
            payload: payload,
            queuedAt: Date.now()
        };
        return window.mwBag.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                if (!db.objectStoreNames.contains('outbox')) {
                    resolve(item.id);
                    return;
                }
                var request = db.transaction('outbox', 'readwrite').objectStore('outbox').put(item);
                request.onsuccess = function () { resolve(item.id); };
                request.onerror = function () { reject(request.error); };
            });
        });
    },
    pending: function () {
        return window.mwBag.open().then(function (db) {
            return new Promise(function (resolve) {
                if (!db.objectStoreNames.contains('outbox')) {
                    resolve([]);
                    return;
                }
                var request = db.transaction('outbox', 'readonly').objectStore('outbox').getAll();
                request.onsuccess = function () { resolve(request.result || []); };
                request.onerror = function () { resolve([]); };
            });
        }).catch(function () { return []; });
    },
    remove: function (id) {
        return window.mwBag.open().then(function (db) {
            return new Promise(function (resolve, reject) {
                if (!db.objectStoreNames.contains('outbox')) {
                    resolve();
                    return;
                }
                var request = db.transaction('outbox', 'readwrite').objectStore('outbox').delete(id);
                request.onsuccess = function () { resolve(); };
                request.onerror = function () { reject(request.error); };
            });
        }).catch(function () { });
    },
    flush: function () {
        return this.pending().then(function (items) {
            window.dispatchEvent(new CustomEvent('mw-outbox', { detail: items }));
            var mail = items.find(function (item) {
                return (item.kind === 'contact' || item.kind === 'newsletter') && item.payload && item.payload.mailto;
            });
            if (mail) {
                window.mwOutbox.remove(mail.id);
                window.location.href = mail.payload.mailto;
            }
            return items;
        });
    }
};

(function () {
    function applyOffline(offline) {
        document.body.classList.toggle('mw-offline', !!offline);
        var banner = document.getElementById('mw-offline-banner');
        if (!banner) {
            banner = document.createElement('div');
            banner.id = 'mw-offline-banner';
            banner.setAttribute('role', 'status');
            banner.textContent = 'You are offline. Saved pages and your bag still open. Sign-in and payment need a connection. Messages wait and send when you are back.';
            document.body.prepend(banner);
        }
        banner.hidden = !offline;
    }

    window.mwSyncBag = function () {
        var bridge = window.__mwStorefrontBridge;
        if (!bridge) return Promise.resolve();
        return bridge.invokeMethodAsync('SyncLocalBag').catch(function () { });
    };

    window.addEventListener('offline', function () { applyOffline(true); });
    window.addEventListener('online', function () {
        applyOffline(false);
        window.mwSyncBag();
        if (window.mwOutbox) window.mwOutbox.flush();
    });
    applyOffline(!navigator.onLine);

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/service-worker.js?v=20261022a').catch(function () { });
    }
})();
