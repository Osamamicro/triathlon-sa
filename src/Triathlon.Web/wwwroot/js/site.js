/* ============================================================
   Saudi Triathlon Federation — shared public-site behaviour.
   Ported from the prototype's assets/js/app.js. The header and
   footer are rendered by the server now, and the language comes
   from the URL, so neither is built or persisted here: what is
   left is the theme toggle, the burger menu, scroll reveal, the
   stat counters and the shared card/date helpers.
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

  /* ---------------- language ----------------
     The culture is a route segment, so the served document already carries the
     right lang/dir. Nothing here writes it, and nothing reads it from storage. */
  function currentLang() { return document.documentElement.lang === "ar" ? "ar" : "en"; }

  /* ---------------- bilingual text helper ---------------- */
  function bi(obj) {
    if (!obj) return "";
    return '<span class="en">' + obj.en + '</span><span class="ar">' + obj.ar + "</span>";
  }

  /* ---------------- date formatting ---------------- */
  const AR_LOCALE = "ar-SA-u-ca-gregory";
  function fmtDate(iso) {
    const d = new Date(iso + "T12:00:00");
    return {
      en: {
        day: d.toLocaleDateString("en-GB", { day: "2-digit" }),
        mon: d.toLocaleDateString("en-GB", { month: "short" }),
        full: d.toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "long", year: "numeric" })
      },
      ar: {
        day: d.toLocaleDateString(AR_LOCALE, { day: "2-digit" }),
        mon: d.toLocaleDateString(AR_LOCALE, { month: "short" }),
        full: d.toLocaleDateString(AR_LOCALE, { weekday: "long", day: "numeric", month: "long", year: "numeric" })
      }
    };
  }
  function fmtMonth(iso) {
    const d = new Date(iso + "T12:00:00");
    return {
      en: d.toLocaleDateString("en-GB", { month: "long", year: "numeric" }),
      ar: d.toLocaleDateString(AR_LOCALE, { month: "long", year: "numeric" })
    };
  }

  /* ---------------- shared chip labels ---------------- */
  const LABELS = {
    competition: { en: "Competition", ar: "بطولات" },
    community:   { en: "Community",  ar: "مجتمعي" },
    open:  { en: "Registration open", ar: "التسجيل مفتوح" },
    soon:  { en: "Opens soon",        ar: "يفتح قريباً" },
    done:  { en: "Completed",         ar: "انتهت" },
    swim: { en: "Swim", ar: "سباحة" },
    bike: { en: "Bike", ar: "دراجة" },
    run:  { en: "Run",  ar: "جري" }
  };

  function typeChip(e) {
    return '<span class="chip chip-' + e.type + '">' + bi(LABELS[e.type]) + "</span>";
  }
  function statusChip(e) {
    const cls = e.status === "open" ? "chip-open" : e.status === "done" ? "chip-done" : "";
    return '<span class="chip ' + cls + '">' + bi(LABELS[e.status]) + "</span>";
  }
  function distanceRow(e) {
    let out = '<div class="distances">';
    if (e.distances.swim) out += '<span class="d-swim">' + bi(LABELS.swim) + " " + e.distances.swim + "</span>";
    if (e.distances.bike) out += '<span class="d-bike">' + bi(LABELS.bike) + " " + e.distances.bike + "</span>";
    if (e.distances.run)  out += '<span class="d-run">'  + bi(LABELS.run)  + " " + e.distances.run  + "</span>";
    return out + "</div>";
  }

  /* ---------------- event card ---------------- */
  function eventCard(e, opts) {
    opts = opts || {};
    const d = fmtDate(e.date);
    const city = STF.cities[e.city];
    const past = e.status === "done";
    /* every public URL is culture-first, so a card links inside the culture it was rendered in */
    const href = "/" + currentLang() + "/events/" + e.id;
    return (
      '<a class="card event-card reveal' + (past ? " past" : "") + '" href="' + href + '">' +
        '<div class="date-block">' +
          '<span class="date-tile">' +
            '<span class="d-m mono"><span class="en">' + d.en.mon + '</span><span class="ar">' + d.ar.mon + "</span></span>" +
            '<span class="d-d"><span class="en">' + d.en.day + '</span><span class="ar">' + d.ar.day + "</span></span>" +
          "</span>" +
          '<div class="chips">' + typeChip(e) + statusChip(e) + "</div>" +
        "</div>" +
        "<h3>" + bi(e.title) + "</h3>" +
        '<div class="event-meta">' +
          "<span>◈ " + bi(city.name) + "</span>" +
          '<span class="mono">' + e.time + "</span>" +
        "</div>" +
        distanceRow(e) +
        '<div class="card-cta"><span>' +
          bi(past ? { en: "Results & recap", ar: "النتائج والملخص" } : { en: "Event page", ar: "صفحة الفعالية" }) +
        "</span><span aria-hidden=\"true\" class=\"cta-arrow\">→</span></div>" +
      "</a>"
    );
  }

  /* ---------------- counters ---------------- */
  function animateCounter(el) {
    const target = parseFloat(el.dataset.count || "0");
    const suffix = el.dataset.suffix || "";
    const plus = el.dataset.plus !== undefined;
    const render = v => {
      el.innerHTML = Math.round(v).toLocaleString(currentLang() === "ar" ? AR_LOCALE : "en-US") +
        suffix + (plus ? '<span class="plus">+</span>' : "");
    };
    if (reduced) { render(target); return; }
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
  }

  /* public API for pages */
  window.STFApp = { bi, fmtDate, fmtMonth, eventCard, typeChip, statusChip, distanceRow, LABELS, currentLang };

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot);
  else boot();
})();
