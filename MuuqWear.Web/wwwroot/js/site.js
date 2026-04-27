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

// =============================================
// CART COOKIE FUNCTIONS
// =============================================

// get cart cookie ✅
// returns JSON string of cart items or null
window.getCartCookie = function () {
    try {
        // get all cookies as string
        // e.g. "muuqwear_auth=xxx; muuqwear_cart=[...]"
        var cookies = document.cookie.split(';');

        for (var i = 0; i < cookies.length; i++) {
            var cookie = cookies[i].trim();

            // find cart cookie by name ✅
            if (cookie.startsWith('muuqwear_cart=')) {
                // extract value after "muuqwear_cart="
                var value = cookie.substring('muuqwear_cart='.length);

                // decode URI component ✅
                // cookie values are URL encoded
                return decodeURIComponent(value);
            }
        }

        // cart cookie not found → return null ✅
        return null;
    }
    catch (e) {
        console.error('getCartCookie error:', e);
        return null;
    }
};

// set cart cookie ✅
// json = JSON string of cart items
// days = cookie expiry in days (default 7)
window.setCartCookie = function (json, days) {
    try {
        // calculate expiry date ✅
        var expires = new Date();
        expires.setDate(expires.getDate() + (days || 7));

        // encode JSON to be safe in cookie ✅
        // handles special characters like []{}",
        var encoded = encodeURIComponent(json);

        // set cookie with:
        // name=value → muuqwear_cart=[...]
        // expires → 7 days ✅
        // path=/ → available on all pages ✅
        // SameSite=Lax → security ✅
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

// clear cart cookie ✅
// called after merging guest cart on login
window.clearCartCookie = function () {
    try {
        // set expiry to past date → browser deletes cookie ✅
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
