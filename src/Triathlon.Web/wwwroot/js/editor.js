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

        // The record is stored before any timer is set and mutated in place from here on, so
        // destroy() always sees the live pending timer through this same object — a plain local
        // `timer` variable captured by value into the instances record at the end of create() would
        // freeze at its initial null, and destroy() would clear nothing, leaving a debounced
        // callback free to fire invokeMethodAsync on a DotNetObjectReference the component has
        // already disposed.
        const record = { quill: quill, timer: null };
        instances[elementId] = record;

        quill.on("text-change", function () {
            if (record.timer) {
                window.clearTimeout(record.timer);
            }
            record.timer = window.setTimeout(function () {
                record.timer = null;
                dotNetRef.invokeMethodAsync("OnHtmlChanged", quill.root.innerHTML);
            }, DEBOUNCE_MS);
        });
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
