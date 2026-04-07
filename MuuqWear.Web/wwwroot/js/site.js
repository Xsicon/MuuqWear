window.focusElement = (element) => {
    if (element) {
        element.focus();
    }
};

window.disableScroll = () => {
    document.body.classList.add("no-scroll");
};

window.enableScroll = () => {
    document.body.classList.remove("no-scroll");
};

