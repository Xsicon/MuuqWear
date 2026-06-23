function mwGetCarouselStep(el, itemSelector) {
    const card = el.querySelector(itemSelector);
    if (!card) return null;

    const row = card.parentElement;
    const style = row ? window.getComputedStyle(row) : null;
    const gap = style ? parseFloat(style.columnGap || style.gap) || 0 : 0;

    return card.getBoundingClientRect().width + gap;
}

function mwIsSingleSlideView(el, itemSelector) {
    const first = el.querySelector(itemSelector || el?._mwItemSelector);
    if (!first) return false;
    return first.getBoundingClientRect().width >= el.clientWidth * 0.85;
}

function mwGetMaxScroll(el, itemSelector) {
    const selector = itemSelector || el?._mwItemSelector;
    if (!selector) {
        return Math.max(0, el.scrollWidth - el.clientWidth);
    }

    const items = mwGetCarouselItems(el, itemSelector);
    if (!items.length) return 0;

    const last = items[items.length - 1];
    const lastOffset = mwGetItemScrollLeft(last);
    const trackRect = el.getBoundingClientRect();
    const lastRect = last.getBoundingClientRect();
    const lastRight = (lastRect.right - trackRect.left) + el.scrollLeft;
    const geometricMax = lastRight - el.clientWidth;

    if (mwIsSingleSlideView(el, itemSelector)) {
        return Math.max(0, lastOffset);
    }

    return Math.max(0, geometricMax);
}

function mwClampHorizontalScroll(el, itemSelector) {
    const max = mwGetMaxScroll(el, itemSelector);
    if (el.scrollLeft < 0) el.scrollLeft = 0;
    else if (el.scrollLeft > max) el.scrollLeft = max;
}

window.mwScrollRight = (el, itemSelector) => {
    mwClampHorizontalScroll(el, itemSelector);

    const maxScroll = mwGetMaxScroll(el, itemSelector);
    if (maxScroll <= 0 || el.scrollLeft >= maxScroll - 5) return;

    const index = mwGetScrollIndex(el, itemSelector);
    const maxIndex = mwGetMaxScrollIndex(el, itemSelector);
    if (index >= maxIndex) return;

    mwScrollToIndex(el, itemSelector, index + 1);
};

window.mwScrollLeft = (el, itemSelector) => {
    mwClampHorizontalScroll(el, itemSelector);

    if (el.scrollLeft <= 5) return;

    const index = mwGetScrollIndex(el, itemSelector);
    if (index <= 0) return;

    mwScrollToIndex(el, itemSelector, index - 1);
};

function mwGetCarouselItems(el, itemSelector) {
    const selector = itemSelector || el?._mwItemSelector;
    if (!selector) return [];
    return Array.from(el.querySelectorAll(selector));
}

function mwGetItemScrollLeft(item) {
    return item?.offsetLeft ?? 0;
}

function mwGetScrollIndex(el, itemSelector) {
    const items = mwGetCarouselItems(el, itemSelector);
    if (!items.length) return 0;

    const scrollLeft = el.scrollLeft;
    let best = 0;
    let bestDist = Infinity;

    for (let i = 0; i < items.length; i++) {
        const dist = Math.abs(mwGetItemScrollLeft(items[i]) - scrollLeft);
        if (dist < bestDist) {
            bestDist = dist;
            best = i;
        }
    }

    return best;
}

function mwGetMaxScrollIndex(el, itemSelector) {
    const items = mwGetCarouselItems(el, itemSelector);
    if (!items.length) return 0;

    if (mwIsSingleSlideView(el, itemSelector)) {
        return items.length - 1;
    }

    const max = mwGetMaxScroll(el, itemSelector);
    let maxIndex = 0;

    for (let i = 0; i < items.length; i++) {
        if (mwGetItemScrollLeft(items[i]) <= max + 2) {
            maxIndex = i;
        }
    }

    return maxIndex;
}

function mwScrollToIndex(el, itemSelector, index, behavior = "smooth") {
    const items = mwGetCarouselItems(el, itemSelector);
    if (!items.length) return;

    const maxIndex = mwGetMaxScrollIndex(el, itemSelector);
    const targetIndex = Math.max(0, Math.min(index, maxIndex));
    const item = items[targetIndex];
    if (!item) return;

    const target = mwGetItemScrollLeft(item);

    if (Math.abs(el.scrollLeft - target) <= 2) {
        el.scrollLeft = target;
        return;
    }

    el.scrollTo({ left: target, behavior });
}

function mwSnapTouchCarousel(trackEl, itemSelector) {
    const startIndex = trackEl._mwTouchStartIndex ?? mwGetScrollIndex(trackEl, itemSelector);
    const currentIndex = mwGetScrollIndex(trackEl, itemSelector);
    const delta = currentIndex - startIndex;

    let targetIndex;
    if (Math.abs(delta) <= 1) {
        targetIndex = currentIndex;
    } else {
        targetIndex = startIndex + (delta > 0 ? 1 : -1);
    }

    mwScrollToIndex(trackEl, itemSelector, targetIndex);
    mwClampHorizontalScroll(trackEl, itemSelector);
    delete trackEl._mwTouchStartIndex;
}

function mwIsTouchDevice() {
    return window.matchMedia("(hover: none) and (pointer: coarse)").matches;
}

window.mwInitCarouselNav = (trackEl, itemSelector) => {
    if (!trackEl) return;
    trackEl._mwItemSelector = itemSelector;

    const inner = trackEl.closest(".acc-carousel-section__inner");
    const prevBtn = inner?.querySelector("[data-carousel-prev]");
    const nextBtn = inner?.querySelector("[data-carousel-next]");

    if (trackEl._mwNavScrollHandler) {
        trackEl.removeEventListener("scroll", trackEl._mwNavScrollHandler);
    }
    if (trackEl._mwNavScrollEndHandler) {
        trackEl.removeEventListener("scrollend", trackEl._mwNavScrollEndHandler);
    }
    if (trackEl._mwNavResizeHandler) {
        window.removeEventListener("resize", trackEl._mwNavResizeHandler);
    }
    if (trackEl._mwNavScrollEndTimer) {
        clearTimeout(trackEl._mwNavScrollEndTimer);
    }

    function updateButtons() {
        const max = mwGetMaxScroll(trackEl, itemSelector);
        const atStart = trackEl.scrollLeft <= 5;
        const atEnd = max <= 5 || trackEl.scrollLeft >= max - 5;
        if (prevBtn) prevBtn.disabled = atStart;
        if (nextBtn) nextBtn.disabled = atEnd;
    }

    function clampAfterScroll() {
        mwClampHorizontalScroll(trackEl, itemSelector);
        updateButtons();
    }

    trackEl._mwNavScrollHandler = () => {
        updateButtons();
        if (mwIsTouchDevice()) return;
        if (trackEl._mwNavScrollEndTimer) {
            clearTimeout(trackEl._mwNavScrollEndTimer);
        }
        trackEl._mwNavScrollEndTimer = setTimeout(clampAfterScroll, 120);
    };

    trackEl._mwNavScrollEndHandler = clampAfterScroll;
    trackEl._mwNavResizeHandler = () => {
        clampAfterScroll();
    };

    trackEl.addEventListener("scroll", trackEl._mwNavScrollHandler, { passive: true });
    trackEl.addEventListener("scrollend", trackEl._mwNavScrollEndHandler, { passive: true });
    window.addEventListener("resize", trackEl._mwNavResizeHandler);
    trackEl.scrollLeft = 0;
    clampAfterScroll();
};

window.mwInitAccessoryCarousel = (trackEl, itemSelector) => {
    mwInitCarouselNav(trackEl, itemSelector);

    if (trackEl._mwTouchStartHandler) {
        trackEl.removeEventListener("touchstart", trackEl._mwTouchStartHandler);
    }

    if (trackEl._mwNavScrollEndHandler) {
        trackEl.removeEventListener("scrollend", trackEl._mwNavScrollEndHandler);
    }
    if (trackEl._mwNavScrollHandler) {
        trackEl.removeEventListener("scroll", trackEl._mwNavScrollHandler);
    }

    function updateNavButtons() {
        const inner = trackEl.closest(".acc-carousel-section__inner");
        const prevBtn = inner?.querySelector("[data-carousel-prev]");
        const nextBtn = inner?.querySelector("[data-carousel-next]");
        const max = mwGetMaxScroll(trackEl, itemSelector);
        const atStart = trackEl.scrollLeft <= 5;
        const atEnd = max <= 5 || trackEl.scrollLeft >= max - 5;
        if (prevBtn) prevBtn.disabled = atStart;
        if (nextBtn) nextBtn.disabled = atEnd;
    }

    trackEl._mwNavScrollHandler = () => {
        const max = mwGetMaxScroll(trackEl, itemSelector);
        if (trackEl.scrollLeft > max + 1) {
            trackEl.scrollLeft = max;
        } else if (trackEl.scrollLeft < 0) {
            trackEl.scrollLeft = 0;
        }
        updateNavButtons();
    };
    trackEl.addEventListener("scroll", trackEl._mwNavScrollHandler, { passive: true });

    function finishScroll() {
        if (mwIsTouchDevice()) {
            mwSnapTouchCarousel(trackEl, itemSelector);
        } else {
            mwClampHorizontalScroll(trackEl, itemSelector);
            const index = mwGetScrollIndex(trackEl, itemSelector);
            mwScrollToIndex(trackEl, itemSelector, index, "auto");
        }

        mwClampHorizontalScroll(trackEl, itemSelector);
        updateNavButtons();
    }

    trackEl._mwNavScrollEndHandler = finishScroll;
    trackEl.addEventListener("scrollend", trackEl._mwNavScrollEndHandler, { passive: true });

    if (mwIsTouchDevice()) {
        trackEl._mwTouchStartHandler = () => {
            trackEl._mwTouchStartIndex = mwGetScrollIndex(trackEl, itemSelector);
        };
        trackEl.addEventListener("touchstart", trackEl._mwTouchStartHandler, { passive: true });
    } else {
        enableDragScroll(trackEl);
    }

    finishScroll();
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
    if (!element || element._mwDragScrollInit) return;
    element._mwDragScrollInit = true;

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

    // Touch: use native momentum scrolling (custom touch handlers cause mobile jank).
};

// Journal overlay helpers (avoid eval usage)
window.mwSetBodyOverflowHidden = () => {
    try { document.body.style.overflow = "hidden"; } catch { }
};

window.mwClearBodyOverflow = () => {
    try { document.body.style.overflow = ""; } catch { }
};