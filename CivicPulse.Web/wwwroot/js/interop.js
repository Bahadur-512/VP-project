window.CivicInterop = {
    _dropdownRefs: {},
    _clickHandlerAdded: false,

    registerDropdown: function (id, dotNetRef) {
        window.CivicInterop._dropdownRefs[id] = dotNetRef;
        if (!window.CivicInterop._clickHandlerAdded) {
            document.addEventListener('click', window.CivicInterop._onDocumentClick);
            window.CivicInterop._clickHandlerAdded = true;
        }
    },

    unregisterDropdown: function (id) {
        delete window.CivicInterop._dropdownRefs[id];
    },

    closeAllDropdowns: function (excludeId) {
        var refs = window.CivicInterop._dropdownRefs;
        for (var id in refs) {
            if (id !== excludeId) {
                refs[id].invokeMethodAsync('CloseDropdown');
            }
        }
    },

    _onDocumentClick: function (e) {
        var refs = window.CivicInterop._dropdownRefs;
        var ids = Object.keys(refs);
        if (ids.length === 0) return;
        var clickedInside = false;
        for (var i = 0; i < ids.length; i++) {
            var el = document.getElementById('fdd-' + ids[i]);
            if (el && el.contains(e.target)) {
                clickedInside = true;
                break;
            }
        }
        if (!clickedInside) {
            for (var i = 0; i < ids.length; i++) {
                refs[ids[i]].invokeMethodAsync('CloseDropdown');
            }
        }
    },

    setCookie: function (name, value, days) {
        var expires = '';
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = '; expires=' + date.toUTCString();
        }
        document.cookie = name + '=' + (value || '') + expires + '; path=/';
    },

    getCookie: function (name) {
        var nameEQ = name + '=';
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) === ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    },

    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    focusElement: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) el.focus();
    },

    downloadFile: function (fileName, base64Content, contentType) {
        var byteCharacters = atob(base64Content);
        var byteNumbers = new Array(byteCharacters.length);
        for (var i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        var byteArray = new Uint8Array(byteNumbers);
        var blob = new Blob([byteArray], { type: contentType || 'application/octet-stream' });
        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }
};
