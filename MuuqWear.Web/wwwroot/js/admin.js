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
