// Small page-wide helpers for the dashboard's own screens (as opposed to editor.js, which only
// bridges BilingualRichText to Quill). Loaded as a plain <script> from App.razor — no inline
// scripts anywhere, the dashboard's CSP is script-src 'self'.
window.stfDashboard = (function () {
    "use strict";

    /** Copies text to the clipboard, e.g. a media asset's public path from the Media screen. */
    async function copyText(text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    }

    return { copyText };
})();
