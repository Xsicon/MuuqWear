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
            slidesPerView: 1.05,
            spaceBetween: 16,
            breakpoints: {
                576: { slidesPerView: 1.15, spaceBetween: 20 },
                768: { slidesPerView: 2, spaceBetween: 26 },
                1024: { slidesPerView: 3, spaceBetween: 26 }
            }
        },
        product: {
            slidesPerView: 1.2,
            spaceBetween: 16,
            breakpoints: {
                576: { slidesPerView: 1.5, spaceBetween: 20 },
                768: { slidesPerView: 2.2, spaceBetween: 26 },
                1024: { slidesPerView: 4, spaceBetween: 26 }
            }
        },
        collection: {
            slidesPerView: 1.05,
            spaceBetween: 16,
            breakpoints: {
                576: { slidesPerView: 1.15, spaceBetween: 20 },
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
            watchOverflow: slideCount <= 1
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

// =============================================
// CART COOKIE FUNCTIONS
// =============================================

// get cart cookie 
// returns JSON string of cart items or null
window.getCartCookie = function () {
    try {
        // get all cookies as string
        // e.g. "muuqwear_auth=xxx; muuqwear_cart=[...]"
        var cookies = document.cookie.split(';');

        for (var i = 0; i < cookies.length; i++) {
            var cookie = cookies[i].trim();

            // find cart cookie by name 
            if (cookie.startsWith('muuqwear_cart=')) {
                // extract value after "muuqwear_cart="
                var value = cookie.substring('muuqwear_cart='.length);

                // decode URI component 
                // cookie values are URL encoded
                return decodeURIComponent(value);
            }
        }

        // cart cookie not found → return null 
        return null;
    }
    catch (e) {
        console.error('getCartCookie error:', e);
        return null;
    }
};

// set cart cookie 
// json = JSON string of cart items
// days = cookie expiry in days (default 7)
window.setCartCookie = function (json, days) {
    try {
        // calculate expiry date 
        var expires = new Date();
        expires.setDate(expires.getDate() + (days || 7));

        // encode JSON to be safe in cookie 
        // handles special characters like []{}",
        var encoded = encodeURIComponent(json);

        // set cookie with:
        // name=value → muuqwear_cart=[...]
        // expires → 7 days 
        // path=/ → available on all pages 
        // SameSite=Lax → security 
        document.cookie =
            'muuqwear_cart=' + encoded +
            '; expires=' + expires.toUTCString() +
            '; path=/' +
            '; SameSite=Lax';
    }
    catch (e) {
        console.error('setCartCookie error:', e);
    }
};

// clear cart cookie 
// called after merging guest cart on login
window.clearCartCookie = function () {
    try {
        // set expiry to past date → browser deletes cookie 
        document.cookie =
            'muuqwear_cart=' +
            '; expires=Thu, 01 Jan 1970 00:00:00 UTC' +
            '; path=/' +
            '; SameSite=Lax';
    }
    catch (e) {
        console.error('clearCartCookie error:', e);
    }
};

// Sign in via single POST — no cache key round-trip (avoids json=1 fetch race).
window.mwSignIn = function (session) {
    return fetch("/auth/sign-in", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(session)
    })
        .then(function (res) { return res.ok; })
        .catch(function (e) {
            console.error("mwSignIn error:", e);
            return false;
        });
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

    start: function () {
        window.mwNavProgress._startedAt = Date.now();
        document.body.classList.add("mw-nav-loading");
    },

    done: function () {
        var elapsed = Date.now() - (window.mwNavProgress._startedAt || 0);
        var remaining = Math.max(0, 500 - elapsed);

        window.setTimeout(function () {
            document.body.classList.remove("mw-nav-loading");
        }, remaining);
    },

    init: function () {
        if (window.mwNavProgress._initialized) return;
        window.mwNavProgress._initialized = true;

        document.addEventListener("click", function (e) {
            var link = e.target.closest("a[href]");
            if (!link || link.target === "_blank" || link.hasAttribute("download")) return;

            var href = link.getAttribute("href");
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
