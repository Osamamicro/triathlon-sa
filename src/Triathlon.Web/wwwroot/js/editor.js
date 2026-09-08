// Bridges BilingualRichText.razor to the vendored Quill editor (wwwroot/lib/quill/). Loaded as a
// plain <script> from App.razor — no inline scripts anywhere, the dashboard's CSP is script-src 'self'.
window.stfEditor = (function () {
    "use strict";

    const TOOLBAR = [
        [{ header: [2, 3, false] }],
        ["bold", "italic", "underline"],
        [{ list: "bullet" }, { list: "ordered" }],
        ["link", "blockquote"],
        ["clean"],
    ];

    const DEBOUNCE_MS = 300;

    /** elementId -> { quill, timer } for every live editor, so destroy() can find its own. */
    const instances = {};

    function create(elementId, initialHtml, isRtl, dotNetRef) {
        const host = document.getElementById(elementId);
        if (!host) {
            return;
        }

        const quill = new Quill(host, {
            theme: "snow",
            modules: { toolbar: TOOLBAR },
        });

        quill.root.innerHTML = initialHtml || "";
        quill.root.setAttribute("dir", isRtl ? "rtl" : "ltr");

        let timer = null;
        quill.on("text-change", function () {
            if (timer) {
                window.clearTimeout(timer);
            }
            timer = window.setTimeout(function () {
                dotNetRef.invokeMethodAsync("OnHtmlChanged", quill.root.innerHTML);
            }, DEBOUNCE_MS);
        });

        instances[elementId] = { quill: quill, timer: timer };
    }

    function destroy(elementId) {
        const instance = instances[elementId];
        if (!instance) {
            return;
        }

        if (instance.timer) {
            window.clearTimeout(instance.timer);
        }

        delete instances[elementId];
    }

    return { create: create, destroy: destroy };
})();
