function mwGetCarouselStep(el, itemSelector) {
    const card = el.querySelector(itemSelector);
    if (!card) return null;

    const row = card.parentElement;
    const style = row ? window.getComputedStyle(row) : null;
    const gap = style ? parseFloat(style.columnGap || style.gap) || 0 : 0;

    return card.getBoundingClientRect().width + gap;
}

function mwGetMaxScroll(el, itemSelector) {
    const selector = itemSelector || el?._mwItemSelector;
    if (!selector) {
        return Math.max(0, el.scrollWidth - el.clientWidth);
    }

    const items = el.querySelectorAll(selector);
    if (!items || items.length === 0) {
        return 0;
    }

    const last = items[items.length - 1];
    const trackRect = el.getBoundingClientRect();
    const lastRect = last.getBoundingClientRect();

    // lastRight in track's scroll coordinate space
    const lastRight = (lastRect.right - trackRect.left) + el.scrollLeft;
    const max = lastRight - el.clientWidth;
    return Math.max(0, Math.ceil(max));
}

function mwClampHorizontalScroll(el, itemSelector) {
    const max = mwGetMaxScroll(el, itemSelector);
    if (el.scrollLeft < 0) el.scrollLeft = 0;
    else if (el.scrollLeft > max) el.scrollLeft = max;
}

window.mwScrollRight = (el, itemSelector) => {
    const step = mwGetCarouselStep(el, itemSelector);
    if (!step) return;

    mwClampHorizontalScroll(el, itemSelector);

    const maxScroll = mwGetMaxScroll(el, itemSelector);
    if (maxScroll <= 0 || el.scrollLeft >= maxScroll - 5) return;

    const index = Math.round(el.scrollLeft / step);

    el.scrollTo({
        left: Math.min((index + 1) * step, maxScroll),
        behavior: "smooth"
    });
};

window.mwScrollLeft = (el, itemSelector) => {
    const step = mwGetCarouselStep(el, itemSelector);
    if (!step) return;

    mwClampHorizontalScroll(el, itemSelector);

    if (el.scrollLeft <= 5) return;

    const index = Math.round(el.scrollLeft / step);

    el.scrollTo({
        left: Math.max((index - 1) * step, 0),
        behavior: "smooth"
    });
};

window.mwInitCarouselNav = (trackEl, itemSelector) => {
    if (!trackEl) return;
    trackEl._mwItemSelector = itemSelector;

    const inner = trackEl.closest(".acc-carousel-section__inner");
    const prevBtn = inner?.querySelector("[data-carousel-prev]");
    const nextBtn = inner?.querySelector("[data-carousel-next]");

    if (trackEl._mwNavScrollHandler) {
        trackEl.removeEventListener("scroll", trackEl._mwNavScrollHandler);
    }
    if (trackEl._mwNavResizeHandler) {
        window.removeEventListener("resize", trackEl._mwNavResizeHandler);
    }

    function update() {
        mwClampHorizontalScroll(trackEl, itemSelector);
        const max = mwGetMaxScroll(trackEl, itemSelector);
        const atStart = trackEl.scrollLeft <= 5;
        const atEnd = max <= 5 || trackEl.scrollLeft >= max - 5;
        if (prevBtn) prevBtn.disabled = atStart;
        if (nextBtn) nextBtn.disabled = atEnd;
    }

    trackEl._mwNavScrollHandler = update;
    trackEl._mwNavResizeHandler = update;
    trackEl.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);
    trackEl.scrollLeft = 0;
    update();
};

window.mwAutoSlideMap = new Map();
window.mwStartAutoSlide = (el, itemSelector, interval = 3000) => {
    if (!el) return;

    if (mwAutoSlideMap.has(el)) return;

    const timer = setInterval(() => {
        const card = el.querySelector(itemSelector);
        if (!card) return;

        const gap = 16;
        const scrollAmount = card.getBoundingClientRect().width + gap;

        const maxScroll = el.scrollWidth - el.clientWidth;

        if (el.scrollLeft >= maxScroll - 5) {
            el.scrollTo({ left: 0, behavior: 'smooth' });
        } else {
            el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        }

    }, interval);

    mwAutoSlideMap.set(el, timer);
};


window.enableDragScroll = (element) => {

    let isDown = false;
    let startX = 0;
    let scrollLeft = 0;
    let isDragging = false;


    element.querySelectorAll("img, a").forEach(el => {
        el.setAttribute("draggable", "false");
    });

    element.addEventListener("dragstart", (e) => {
        e.preventDefault();
    });

    element.addEventListener('mousedown', (e) => {
        isDown = true;
        isDragging = false;

        startX = e.clientX;
        scrollLeft = element.scrollLeft;

        element.classList.add("dragging");

    });

    window.addEventListener('mousemove', (e) => {
        if (!isDown) return;

        const delta = e.clientX - startX;

        // threshold
        if (Math.abs(delta) > 5) {
            if (!isDragging) isDragging = true;
        }

        if (!isDragging) return;

        e.preventDefault();

        const max = mwGetMaxScroll(element);
        element.scrollLeft = Math.max(0, Math.min(scrollLeft - delta, max));

    });

    // (WINDOW LEVEL - FIXES STUCK DRAG)
    window.addEventListener('mouseup', () => {
        if (!isDown) return;
        isDown = false;

        mwClampHorizontalScroll(element);

        // Delay reset (prevents click firing)
        setTimeout(() => {
            isDragging = false;
        }, 50);

        element.classList.remove("dragging");
    });

    // Prevent click when dragging (CAPTURE PHASE)
    element.addEventListener("click", (e) => {
        if (isDragging) {
            e.preventDefault();
            e.stopPropagation();
        }
    }, true);

    // ======================
    // TOUCH SUPPORT (MOBILE)
    // ======================

    element.addEventListener('touchstart', (e) => {
        startX = e.touches[0].clientX;
        scrollLeft = element.scrollLeft;
        isDragging = false;

    });

    element.addEventListener('touchmove', (e) => {
        const delta = e.touches[0].clientX - startX;

        if (Math.abs(delta) > 5) {
            isDragging = true;
        }

        if (!isDragging) return;

        const max = mwGetMaxScroll(element);
        element.scrollLeft = Math.max(0, Math.min(scrollLeft - delta, max));

    });

    element.addEventListener('touchend', () => {
        mwClampHorizontalScroll(element);

        setTimeout(() => {
            isDragging = false;
        }, 50);

    });
};

// Journal overlay helpers (avoid eval usage)
window.mwSetBodyOverflowHidden = () => {
    try { document.body.style.overflow = "hidden"; } catch { }
};

window.mwClearBodyOverflow = () => {
    try { document.body.style.overflow = ""; } catch { }
};