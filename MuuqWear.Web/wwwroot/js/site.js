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
