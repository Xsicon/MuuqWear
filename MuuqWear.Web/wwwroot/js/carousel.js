window.mwScrollRight = (el, itemSelector) => {
    const card = el.querySelector(itemSelector);
    if (!card) return;

    const scrollAmount = card.offsetWidth;

    const index = Math.round(el.scrollLeft / scrollAmount);

    el.scrollTo({
        left: (index + 1) * scrollAmount,
        behavior: "smooth"
    });
};

window.mwScrollLeft = (el, itemSelector) => {
    const card = el.querySelector(itemSelector);
    if (!card) return;

    const scrollAmount = card.offsetWidth;

    const index = Math.round(el.scrollLeft / scrollAmount);

    el.scrollTo({
        left: (index - 1) * scrollAmount,
        behavior: "smooth"
    });
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

        element.scrollLeft = scrollLeft - delta;

    });

    // (WINDOW LEVEL - FIXES STUCK DRAG)
    window.addEventListener('mouseup', () => {
        if (!isDown) return;
        isDown = false;

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

        element.scrollLeft = scrollLeft - delta;

    });

    element.addEventListener('touchend', () => {
        setTimeout(() => {
            isDragging = false;
        }, 50);

    });
};