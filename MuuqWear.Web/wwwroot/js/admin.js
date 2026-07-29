window.adminSidebar = {
    _root: function () {
        return document.querySelector('.mw-admin');
    },

    _readCookieTheme: function () {
        var match = document.cookie.match(/(?:^|;\s*)admin-theme=(dark|light)/);
        return match ? match[1] : null;
    },

    _setCookieTheme: function (value) {
        document.cookie = 'admin-theme=' + value + ';path=/;max-age=31536000;SameSite=Lax';
    },

    _getStoredTheme: function () {
        try {
            var stored = localStorage.getItem('admin-theme');
            if (stored === 'dark' || stored === 'light') {
                return stored;
            }
        } catch (e) { }

        var fromCookie = this._readCookieTheme();
        if (fromCookie) {
            return fromCookie;
        }

        var fromHtml = document.documentElement.getAttribute('data-admin-theme');
        if (fromHtml === 'dark' || fromHtml === 'light') {
            return fromHtml;
        }

        return 'light';
    },

    _applyTheme: function (theme) {
        var value = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-admin-theme', value);

        var root = this._root();
        if (root) {
            root.setAttribute('data-theme', value);
        }

        try {
            localStorage.setItem('admin-theme', value);
        } catch (e) { }

        this._setCookieTheme(value);
    },

    init: function () {
        this._applyTheme(this._getStoredTheme());

        var root = this._root();
        if (!root) {
            return;
        }

        if (localStorage.getItem('admin-sidebar-collapsed') === '1') {
            root.classList.add('an-sidebar-collapsed');
        }
    },

    /* Mobile drawer */
    toggle: function () {
        var sidebar = document.querySelector('.an-sidebar');
        var overlay = document.querySelector('.an-overlay');
        if (sidebar) sidebar.classList.toggle('an-sidebar--open');
        if (overlay) overlay.classList.toggle('an-overlay--visible');
    },

    close: function () {
        var sidebar = document.querySelector('.an-sidebar');
        var overlay = document.querySelector('.an-overlay');
        if (sidebar) sidebar.classList.remove('an-sidebar--open');
        if (overlay) overlay.classList.remove('an-overlay--visible');
    },

    /* Desktop icon-rail collapse */
    toggleCollapsed: function () {
        var root = this._root();
        if (!root) return;
        root.classList.toggle('an-sidebar-collapsed');
        localStorage.setItem(
            'admin-sidebar-collapsed',
            root.classList.contains('an-sidebar-collapsed') ? '1' : '0'
        );
    },

    /* Light / dark theme */
    toggleTheme: function () {
        var current = this._getStoredTheme();
        this._applyTheme(current === 'dark' ? 'light' : 'dark');
    }
};

window.showAdminContent = function () {
    var overlay = document.getElementById('admin-auth-overlay');
    if (overlay) overlay.remove();
};

window.initAdminShell = function () {
    window.adminSidebar.init();
};

document.addEventListener('DOMContentLoaded', window.initAdminShell);

window.adminScroll = {
    scrollIntoView: function (elementId) {
        var el = document.getElementById(elementId);
        if (!el) return;
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    },
    scrollElementIntoView: function (element) {
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
    }
};

window.adminHeaderReadState = {
    load: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },
    save: function (key, value) {
        try {
            if (value == null || value === '') {
                localStorage.removeItem(key);
            } else {
                localStorage.setItem(key, value);
            }
        } catch (e) { }
    }
};

window.adminMediaUpload = {
    openPicker: function (inputElement) {
        if (inputElement) {
            inputElement.click();
        }
    }
};

window.mwAdminNav = {
    _initialized: false,
    _loadingEl: null,
    _fallbackTimer: null,

    init: function () {
        if (this._initialized) return;
        this._initialized = true;

        var self = this;

        document.addEventListener('click', function (e) {
            var target = e.target.closest('.an-nav__item, .an-nav__subitem');
            if (!target) return;

            if (target.classList.contains('an-nav-group__header')) return;

            self.setLoading(target);
            if (window.mwNavProgress) window.mwNavProgress.start();
        }, true);

        if (window.mwNavProgress && window.mwNavProgress._bindNavigationEnd) {
            // End handlers are registered once from mwNavProgress.init.
        }
    },

    setLoading: function (el) {
        this.clearLoading();
        el.classList.add('an-nav--loading');
        this._loadingEl = el;
    },

    clearLoading: function () {
        if (this._fallbackTimer) {
            clearTimeout(this._fallbackTimer);
            this._fallbackTimer = null;
        }

        if (this._loadingEl) {
            this._loadingEl.classList.remove('an-nav--loading');
            this._loadingEl = null;
        }

        document.querySelectorAll('.an-nav--loading').forEach(function (el) {
            el.classList.remove('an-nav--loading');
        });
    },

    startFallbackTimeout: function () {
        var self = this;
        if (this._fallbackTimer) clearTimeout(this._fallbackTimer);
        this._fallbackTimer = setTimeout(function () {
            if (window.mwNavProgress) {
                window.mwNavProgress.pageReady();
            } else {
                self.clearLoading();
            }
        }, 12000);
    }
};
