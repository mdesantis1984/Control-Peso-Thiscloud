// ============================================================================
// Storage JavaScript Interop
// Provides unified access to browser storage APIs (localStorage, sessionStorage)
// ============================================================================

window.storageInterop = {
    // ========================================================================
    // localStorage methods
    // ========================================================================

    localStorage: {
        getItem: function (key) {
            try {
                return localStorage.getItem(key);
            } catch (e) {
                console.error('localStorage.getItem error:', e);
                return null;
            }
        },

        setItem: function (key, value) {
            try {
                localStorage.setItem(key, value);
                return true;
            } catch (e) {
                console.error('localStorage.setItem error:', e);
                return false;
            }
        },

        removeItem: function (key) {
            try {
                localStorage.removeItem(key);
                return true;
            } catch (e) {
                console.error('localStorage.removeItem error:', e);
                return false;
            }
        },

        clear: function () {
            try {
                localStorage.clear();
                return true;
            } catch (e) {
                console.error('localStorage.clear error:', e);
                return false;
            }
        },

        length: function () {
            try {
                return localStorage.length;
            } catch (e) {
                console.error('localStorage.length error:', e);
                return 0;
            }
        },

        key: function (index) {
            try {
                return localStorage.key(index);
            } catch (e) {
                console.error('localStorage.key error:', e);
                return null;
            }
        },

        containsKey: function (key) {
            try {
                return localStorage.getItem(key) !== null;
            } catch (e) {
                console.error('localStorage.containsKey error:', e);
                return false;
            }
        }
    },

    // ========================================================================
    // sessionStorage methods
    // ========================================================================

    sessionStorage: {
        getItem: function (key) {
            try {
                return sessionStorage.getItem(key);
            } catch (e) {
                console.error('sessionStorage.getItem error:', e);
                return null;
            }
        },

        setItem: function (key, value) {
            try {
                sessionStorage.setItem(key, value);
                return true;
            } catch (e) {
                console.error('sessionStorage.setItem error:', e);
                return false;
            }
        },

        removeItem: function (key) {
            try {
                sessionStorage.removeItem(key);
                return true;
            } catch (e) {
                console.error('sessionStorage.removeItem error:', e);
                return false;
            }
        },

        clear: function () {
            try {
                sessionStorage.clear();
                return true;
            } catch (e) {
                console.error('sessionStorage.clear error:', e);
                return false;
            }
        },

        length: function () {
            try {
                return sessionStorage.length;
            } catch (e) {
                console.error('sessionStorage.length error:', e);
                return 0;
            }
        },

        key: function (index) {
            try {
                return sessionStorage.key(index);
            } catch (e) {
                console.error('sessionStorage.key error:', e);
                return null;
            }
        },

        containsKey: function (key) {
            try {
                return sessionStorage.getItem(key) !== null;
            } catch (e) {
                console.error('sessionStorage.containsKey error:', e);
                return false;
            }
        }
    }
};
