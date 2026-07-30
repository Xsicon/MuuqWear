window.chatScroll = {
    nearBottomThreshold: 80,

    isNearBottom: function (container) {
        if (!container) return true;
        var threshold = window.chatScroll.nearBottomThreshold;
        return container.scrollHeight - container.scrollTop - container.clientHeight <= threshold;
    },

    scrollToBottom: function (container, force) {
        if (!container) return;

        var shouldScroll = force === true || window.chatScroll.isNearBottom(container);
        if (!shouldScroll) return;

        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                container.scrollTop = container.scrollHeight;
            });
        });
    }
};
