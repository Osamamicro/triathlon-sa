/* ============================================================
   Saudi Triathlon Federation — shared app shell
   Header/footer injection, EN⇄AR toggle (with RTL), scroll
   reveal, stat counters, event-card rendering.
   ============================================================ */

(function () {
  "use strict";

  const LANG_KEY = "stf-lang";
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
      b.setAttribute("aria-label", theme === "light" ? "Switch to dark mode" : "Switch to light mode");
    });
  }

  /* ---------------- language ---------------- */
  function currentLang() { return document.documentElement.lang === "ar" ? "ar" : "en"; }

  function applyLang(lang) {
    const html = document.documentElement;
    html.lang = lang;
    html.dir = lang === "ar" ? "rtl" : "ltr";
    try { localStorage.setItem(LANG_KEY, lang); } catch (e) { /* storage may be blocked */ }
    document.querySelectorAll(".lang-toggle").forEach(b => {
      b.textContent = lang === "ar" ? "English" : "العربية";
      b.setAttribute("aria-label", lang === "ar" ? "Switch to English" : "التبديل إلى العربية");
    });
  }

  let saved = null;
  try { saved = localStorage.getItem(LANG_KEY); } catch (e) { /* ignore */ }

  let savedTheme = null;
  try { savedTheme = localStorage.getItem(THEME_KEY); } catch (e) { /* ignore */ }

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
    return (
      '<a class="card event-card reveal' + (past ? " past" : "") + '" href="event.html?id=' + e.id + '">' +
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

  /* ---------------- header / footer ---------------- */
  const NAV = [
    { href: "index.html",      label: { en: "Home",       ar: "الرئيسية" } },
    { href: "events.html",     label: { en: "Events",     ar: "الفعاليات" } },
    { href: "timeline.html",   label: { en: "Season",     ar: "الموسم" } },
    { href: "join.html",       label: { en: "Join",       ar: "الانضمام" } },
    { href: "training.html",   label: { en: "Training",   ar: "التدريب" } },
    { href: "rules.html",      label: { en: "Rules",      ar: "اللوائح" } },
    { href: "governance.html", label: { en: "Governance", ar: "الحوكمة" } },
    { href: "stats.html",      label: { en: "Statistics", ar: "الإحصائيات" } }
  ];

  const BRAND_MARK =
    '<svg class="brand-mark" viewBox="0 0 44 44" aria-hidden="true">' +
      '<path d="M22 5 L39 37 H33 L22 16 L11 37 H5 Z" fill="none" stroke="url(#triGrad)" stroke-width="3.4" stroke-linejoin="round"/>' +
      "<defs><linearGradient id=\"triGrad\" x1=\"0\" y1=\"0\" x2=\"44\" y2=\"44\">" +
        '<stop offset="0" stop-color="#26C6E8"/><stop offset=".5" stop-color="#2EE68A"/><stop offset="1" stop-color="#FFA245"/>' +
      "</linearGradient></defs>" +
    "</svg>";

  function pageFile() {
    const p = location.pathname.split("/").pop();
    return p === "" ? "index.html" : p;
  }

  function buildHeader() {
    const here = pageFile();
    const links = NAV.map(n => {
      const cur = n.href === here || (here === "event.html" && n.href === "events.html");
      return '<a href="' + n.href + '"' + (cur ? ' aria-current="page"' : "") + ">" + bi(n.label) + "</a>";
    }).join("");
    return (
      '<div class="proto-note">' +
        bi({ en: "Design prototype — all figures and documents are placeholders",
             ar: "نموذج تصميمي — جميع الأرقام والمستندات هنا تجريبية" }) +
      "</div>" +
      '<header class="site-header"><div class="container">' +
        '<a class="brand" href="index.html">' + BRAND_MARK +
          '<span class="brand-name">' +
            bi({ en: "Saudi Triathlon", ar: "الاتحاد السعودي للترايثلون" }) +
            "<small><span class=\"en\">FEDERATION · SWIM BIKE RUN</span><span class=\"ar\">سباحة · دراجة · جري</span></small>" +
          "</span>" +
        "</a>" +
        '<nav class="main-nav" id="mainNav" aria-label="Main">' + links + "</nav>" +
        '<button class="theme-toggle" type="button" aria-label="Switch theme">' +
          '<svg class="i-moon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z"/></svg>' +
          '<svg class="i-sun" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4.2"/><path d="M12 2v2.4M12 19.6V22M4.2 4.2l1.7 1.7M18.1 18.1l1.7 1.7M2 12h2.4M19.6 12H22M4.2 19.8l1.7-1.7M18.1 5.9l1.7-1.7"/></svg>' +
        "</button>" +
        '<button class="lang-toggle" type="button">العربية</button>' +
        '<button class="nav-burger" type="button" aria-expanded="false" aria-controls="mainNav" aria-label="Menu">☰</button>' +
      "</div></header>"
    );
  }

  function buildFooter() {
    const year = new Date().getFullYear();
    return (
      '<footer class="site-footer"><div class="footer-tri"></div><div class="container">' +
        '<div class="footer-grid">' +
          "<div>" +
            '<div class="brand" style="margin-bottom:14px">' + BRAND_MARK +
              '<span class="brand-name">' + bi({ en: "Saudi Triathlon", ar: "الاتحاد السعودي للترايثلون" }) + "</span>" +
            "</div>" +
            '<p class="muted" style="font-size:.9rem;max-width:34ch">' +
              bi({ en: "The national governing body for triathlon, duathlon and aquathlon in the Kingdom of Saudi Arabia.",
                   ar: "الجهة الوطنية المنظمة لرياضات الترايثلون والدواثلون والأكواثلون في المملكة العربية السعودية." }) +
            "</p>" +
          "</div>" +
          "<div><h4>" + bi({ en: "Compete", ar: "المنافسات" }) + "</h4><ul>" +
            '<li><a href="events.html">' + bi({ en: "Events & calendar", ar: "الفعاليات والتقويم" }) + "</a></li>" +
            '<li><a href="timeline.html">' + bi({ en: "Season timeline", ar: "الجدول الزمني للموسم" }) + "</a></li>" +
            '<li><a href="rules.html">' + bi({ en: "Rules & regulations", ar: "اللوائح والأنظمة" }) + "</a></li>" +
            '<li><a href="stats.html">' + bi({ en: "Federation statistics", ar: "إحصائيات الاتحاد" }) + "</a></li>" +
          "</ul></div>" +
          "<div><h4>" + bi({ en: "Get involved", ar: "شارك معنا" }) + "</h4><ul>" +
            '<li><a href="register.html">' + bi({ en: "Athlete registration", ar: "تسجيل الرياضيين" }) + "</a></li>" +
            '<li><a href="join.html">' + bi({ en: "Become an athlete", ar: "كن رياضياً" }) + "</a></li>" +
            '<li><a href="training.html">' + bi({ en: "Training guide", ar: "دليل التدريب" }) + "</a></li>" +
            '<li><a href="join.html#clubs">' + bi({ en: "Affiliated clubs", ar: "الأندية المنتسبة" }) + "</a></li>" +
            '<li><a href="governance.html">' + bi({ en: "Governance & transparency", ar: "الحوكمة والشفافية" }) + "</a></li>" +
          "</ul></div>" +
          "<div><h4>" + bi({ en: "Contact", ar: "تواصل معنا" }) + "</h4><ul>" +
            '<li><a href="https://triathlon.sa" rel="noopener">triathlon.sa</a></li>' +
            '<li><a href="mailto:info@triathlon.sa">info@triathlon.sa</a></li>' +
            '<li><a href="#" rel="noopener">@TriathlonKSA</a></li>' +
          "</ul></div>" +
        "</div>" +
        '<div class="footer-bottom">' +
          "<span>© " + year + " " + bi({ en: "Saudi Triathlon Federation — design prototype", ar: "الاتحاد السعودي للترايثلون — نموذج تصميمي" }) + "</span>" +
          '<a href="admin.html">' + bi({ en: "CONTENT DASHBOARD (DEMO)", ar: "لوحة إدارة المحتوى (تجريبي)" }) + "</a>" +
          '<span class="en">SWIM · BIKE · RUN</span><span class="ar">سباحة · دراجة · جري</span>' +
        "</div>" +
      "</div></footer>"
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
    applyLang(saved === "ar" || saved === "en" ? saved : currentLang());
    const urlTheme = new URLSearchParams(location.search).get("theme");
    const initialTheme = (urlTheme === "light" || urlTheme === "dark") ? urlTheme
      : (savedTheme === "light" ? "light" : "dark");
    applyTheme(initialTheme);

    const headerHost = document.getElementById("site-header");
    const footerHost = document.getElementById("site-footer");
    if (headerHost) headerHost.innerHTML = buildHeader();
    if (footerHost) footerHost.innerHTML = buildFooter();
    applyTheme(currentTheme());   /* refresh the toggle's aria-label after header renders */

    document.addEventListener("click", ev => {
      const t = ev.target.closest(".lang-toggle");
      if (t) { applyLang(currentLang() === "ar" ? "en" : "ar"); document.dispatchEvent(new CustomEvent("stf:lang")); return; }
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
