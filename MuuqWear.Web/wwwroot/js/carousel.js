

//window.mwAutoSlideMap = new Map();

//window.mwStartAutoSlide = (el, interval = 3000) => {
//    if (!el) return;

//    // prevent duplicate timers
//    if (mwAutoSlideMap.has(el)) return;

//    const timer = setInterval(() => {
//        if (!el || el.children.length === 0) return;

//        const card = el.children[0];
//        const gap = 16;

//        const scrollAmount = card.getBoundingClientRect().width + gap;

//        // 👉 if reached end → reset to start (infinite loop feel)
//        const maxScroll = el.scrollWidth - el.clientWidth;

//        if (el.scrollLeft >= maxScroll - 5) {
//            el.scrollTo({ left: 0, behavior: 'smooth' });
//        } else {
//            el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
//        }

//    }, interval);

//    mwAutoSlideMap.set(el, timer);
//};

//window.mwStopAutoSlide = (el) => {
//    const timer = mwAutoSlideMap.get(el);
//    if (timer) {
//        clearInterval(timer);
//        mwAutoSlideMap.delete(el);
//    }
//};


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
window.mwStartAutoSlide = (el,itemSelector, interval = 3000) => {
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




