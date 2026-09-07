/* ============================================================
   Saudi Triathlon Federation — shared public-site behaviour.
   Everything this file used to build as HTML — event cards, chips,
   distance rows, bilingual spans, formatted dates — is rendered by
   the server now, in one culture, from the database. What is left
   is behaviour that has no server-side equivalent: the theme
   toggle, the burger menu, scroll reveal, the stat counters and
   the filter select that submits its own form.

   Deliberately free of innerHTML: with a script-src 'self' policy
   and no markup built here, there is nothing on the page that can
   turn content into script.
   ============================================================ */

(function () {
  "use strict";

  const THEME_KEY = "stf-theme";
  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  /* ---------------- theme (dark default, light opt-in) ---------------- */
  function currentTheme() { return document.documentElement.getAttribute("data-theme") === "light" ? "light" : "dark"; }

  function applyTheme(theme) {
    const html = document.documentElement;
    if (theme === "light") html.setAttribute("data-theme", "light");
    else html.removeAttribute("data-theme");
    try { localStorage.setItem(THEME_KEY, theme); } catch (e) { /* storage may be blocked */ }
    document.querySelectorAll(".theme-toggle").forEach(b => {
      /* labels are server-rendered per culture in data-label-light/dark; leave the
         SSR aria-label alone if a page ever lacks them rather than guessing in English */
      const label = theme === "light" ? b.dataset.labelDark : b.dataset.labelLight;
      if (label) b.setAttribute("aria-label", label);
    });
  }

  /* ---------------- counters ----------------
     The tile already carries the real figure, rendered by the server: a crawler, a printout and
     a no-JS visitor all read it. The count-up is decoration on top, so it resets the number to
     zero itself and only when motion is allowed. Under prefers-reduced-motion this function
     touches nothing and the served value stands. */
  function animateCounter(el) {
    if (reduced) return;
    const target = parseFloat(el.dataset.count || "0");
    const suffix = el.dataset.suffix || "";
    const locale = document.documentElement.lang === "ar" ? "ar-SA-u-ca-gregory" : "en-US";
    /* the number is the element's first text node; a trailing <span class="plus">,
       rendered once by the server, stays untouched */
    const node = el.firstChild && el.firstChild.nodeType === 3
      ? el.firstChild
      : el.insertBefore(document.createTextNode(""), el.firstChild);
    const render = v => { node.nodeValue = Math.round(v).toLocaleString(locale) + suffix; };
    render(0);
    const dur = 1400, t0 = performance.now();
    (function tick(t) {
      const p = Math.min((t - t0) / dur, 1);
      render(target * (1 - Math.pow(1 - p, 3)));
      if (p < 1) requestAnimationFrame(tick);
    })(t0);
  }

  /* ---------------- boot ---------------- */
  function boot() {
    /* theme.js in <head> already painted the theme; this only syncs the
       toggle's aria-label and re-persists the active choice */
    applyTheme(currentTheme());

    document.addEventListener("click", ev => {
      const th = ev.target.closest(".theme-toggle");
      if (th) { applyTheme(currentTheme() === "light" ? "dark" : "light"); document.dispatchEvent(new CustomEvent("stf:theme")); return; }
      const b = ev.target.closest(".nav-burger");
      if (b) {
        const nav = document.getElementById("mainNav");
        const open = nav.classList.toggle("open");
        b.setAttribute("aria-expanded", String(open));
      }
    });

    /* reveal on scroll — resting state is visible; .pre is only added
       right before observation so no-JS and reduced-motion stay readable */
    if (!reduced && "IntersectionObserver" in window) {
      const io = new IntersectionObserver(entries => {
        entries.forEach(en => {
          if (en.isIntersecting) { en.target.classList.remove("pre"); io.unobserve(en.target); }
        });
      }, { threshold: 0.12 });
      document.querySelectorAll(".reveal").forEach(el => {
        const r = el.getBoundingClientRect();
        if (r.top > window.innerHeight * 0.9) { el.classList.add("pre"); io.observe(el); }
      });
    }

    /* counters */
    const counters = document.querySelectorAll("[data-count]");
    if ("IntersectionObserver" in window) {
      const io2 = new IntersectionObserver(entries => {
        entries.forEach(en => {
          if (en.isIntersecting) { animateCounter(en.target); io2.unobserve(en.target); }
        });
      }, { threshold: 0.4 });
      counters.forEach(el => io2.observe(el));
    } else {
      counters.forEach(animateCounter);
    }

    /* ---------------- statistics bars ----------------
       The server writes each bar's real width into the style attribute as well as into data-w,
       so the charts are drawn in the served HTML. Where motion is allowed the bars are collapsed
       to zero without a transition, then grown back as each one scrolls into view. Reduced
       motion, or no IntersectionObserver, leaves the served widths exactly as they are. */
    function setBarWidth(el) { el.style.width = el.dataset.w + "%"; }
    const fills = document.querySelectorAll(".bar-fill");
    if (!reduced && "IntersectionObserver" in window && fills.length) {
      const io3 = new IntersectionObserver(entries => {
        entries.forEach(en => {
          if (en.isIntersecting) { en.target.style.width = ""; setBarWidth(en.target); io3.unobserve(en.target); }
          else if (!en.target.dataset.reset) {
            /* collapse on the observer's first word about this bar, not before it: if the
               observer never reports, the width the server wrote is what stays on screen */
            en.target.dataset.reset = "1";
            en.target.style.transition = "none";
            en.target.style.width = "0%";
            void en.target.offsetWidth;
            en.target.style.transition = "";
          }
        });
      }, { threshold: 0.4 });
      fills.forEach(el => io3.observe(el));
    }

    /* <select data-autosubmit> inside a GET form submits on change; no-JS visitors use
       the noscript button beside it */
    document.addEventListener("change", ev => {
      const s = ev.target.closest("select[data-autosubmit]");
      if (s && s.form) s.form.requestSubmit();
    });
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot);
  else boot();
})();
