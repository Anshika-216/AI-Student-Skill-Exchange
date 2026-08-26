(function () {
    'use strict';

    function host() {
        var el = document.querySelector('.toast-host');
        if (!el) {
            el = document.createElement('div');
            el.className = 'toast-host';
            document.body.appendChild(el);
        }
        return el;
    }

    window.toast = function (message, kind) {
        var el = document.createElement('div');
        el.className = 'toast-msg ' + (kind || '');
        el.textContent = message;
        host().appendChild(el);
        setTimeout(function () { el.remove(); }, 3600);
    };

    window.api = async function (url, options) {
        options = options || {};
        var init = {
            method: options.method || 'GET',
            headers: { 'Accept': 'application/json' },
            credentials: 'same-origin'
        };

        if (options.body !== undefined) {
            init.headers['Content-Type'] = 'application/json';
            init.body = JSON.stringify(options.body);
        }

        var response = await fetch(url, init);

        if (response.status === 401) {
            window.location.href = '/Identity/Account/Login?returnUrl=' +
                encodeURIComponent(window.location.pathname);
            throw new Error('Not signed in');
        }

        var text = await response.text();
        var payload = null;
        if (text) {
            try { payload = JSON.parse(text); } catch (e) { payload = null; }
        }

        if (!response.ok) {
            var message = (payload && (payload.message || payload.title)) ||
                'Request failed (' + response.status + ')';
            throw new Error(message);
        }

        return payload;
    };

    window.escapeHtml = function (value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    };

    window.formatDate = function (value) {
        if (!value) { return ''; }
        var date = new Date(value);
        if (isNaN(date.getTime())) { return ''; }
        return date.toLocaleString(undefined, {
            day: 'numeric', month: 'short', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
    };

    window.forDateTimeLocal = function (value) {
        var date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) { date = new Date(); }
        var pad = function (n) { return String(n).padStart(2, '0'); };
        return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) +
            'T' + pad(date.getHours()) + ':' + pad(date.getMinutes());
    };
})();
