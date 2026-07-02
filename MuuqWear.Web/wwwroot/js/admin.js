window.adminSidebar = {
    _root: function () {
        return document.querySelector('.mw-admin');
    },

    init: function () {
        var root = this._root();
        if (!root) return;

        if (localStorage.getItem('admin-sidebar-collapsed') === '1') {
            root.classList.add('an-sidebar-collapsed');
        }

        var theme = localStorage.getItem('admin-theme') || 'light';
        root.setAttribute('data-theme', theme);
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
        var root = this._root();
        if (!root) return;
        var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-theme', next);
        localStorage.setItem('admin-theme', next);
    }
};

window.showAdminContent = function () {
    var overlay = document.getElementById('admin-auth-overlay');
    if (overlay) overlay.remove();
};

document.addEventListener('DOMContentLoaded', function () {
    window.adminSidebar.init();
});
