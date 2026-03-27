window.carouselInterop = {
    timer: null,
    start: function (dotNetRef, interval) {
        this.timer = setInterval(() => {
            dotNetRef.invokeMethodAsync('AutoAdvance');
        }, interval);
    },
    stop: function () {
        if (this.timer) clearInterval(this.timer);
    }
};