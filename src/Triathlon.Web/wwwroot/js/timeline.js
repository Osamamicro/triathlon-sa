/* ============================================================
   Season timeline — the only behaviour the page needs.

   Every item, month heading and map marker is already in the DOM,
   rendered by the server in one culture. This file never builds
   markup and never touches innerHTML: it toggles classes and the
   `hidden` property on elements that are already there, and writes
   text into one live region, which is what keeps the page working
   without script and the CSP at script-src 'self'.

   Three things light a city, in order of authority:
     1. a city the reader picked, by clicking or pressing Enter on
        its marker — it stays lit until it is picked again;
     2. the card the pointer or the keyboard is on;
     3. otherwise, the event nearest the middle of the viewport, so
        scrolling the season from September to March walks the map
        down the Kingdom on its own.
   ============================================================ */

(function () {
  "use strict";

  const host = document.getElementById("timelineHost");
  const map = document.getElementById("ksaMap");
  if (!host || !map) return;

  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const seg = document.getElementById("typeSeg");
  const live = document.getElementById("timelineLive");
  const items = Array.from(host.querySelectorAll(".tl-item"));
  const months = Array.from(host.querySelectorAll(".tl-month"));
  const markers = Array.from(map.querySelectorAll(".marker"));
  let type = "all", activeCity = null, scrollCity = null;

  function applyFilter() {
    items.forEach(it => { it.hidden = type !== "all" && it.dataset.type !== type; });

    /* a month heading stays only while one of its items is visible */
    months.forEach(m => {
      let el = m.nextElementSibling, visible = false;
      while (el && !el.classList.contains("tl-month")) {
        if (!el.hidden) visible = true;
        el = el.nextElementSibling;
      }
      m.hidden = !visible;
    });

    /* a marker goes only when every calendar it hosts is filtered out */
    markers.forEach(mk => {
      mk.classList.toggle("is-hidden", type !== "all" && !mk.dataset.type.split(" ").includes(type));
    });
  }

  function highlight(city) {
    markers.forEach(mk => mk.classList.toggle("active", mk.dataset.city === city));
    items.forEach(it => it.classList.toggle("active", it.dataset.city === city));
  }

  /* The map's highlight is a visual change, so it is said out loud too. The template comes
     from the server in the page's culture ("{city}, events: {n}"), which keeps the string
     out of this file and out of any plural rule. */
  function announce(city) {
    if (!live || !live.dataset.template) return;
    if (!city) { live.textContent = ""; return; }
    const marker = markers.find(mk => mk.dataset.city === city);
    if (!marker) return;
    const count = items.filter(it => it.dataset.city === city && !it.hidden).length;
    const locale = document.documentElement.lang === "ar" ? "ar-SA-u-ca-gregory" : "en-US";
    live.textContent = live.dataset.template
      .replace("{city}", marker.getAttribute("aria-label") || city)
      .replace("{n}", count.toLocaleString(locale));
  }

  /* What should be lit right now, given everything the reader has done. */
  function refresh() { highlight(activeCity || scrollCity); }

  function setActive(city) {
    activeCity = activeCity === city ? null : city;
    refresh();
    announce(activeCity || scrollCity);
    if (activeCity) {
      const first = host.querySelector('.tl-item[data-city="' + activeCity + '"]:not([hidden])');
      if (first) first.scrollIntoView({ behavior: reduced ? "auto" : "smooth", block: "center" });
    }
  }

  if (seg) {
    seg.addEventListener("click", ev => {
      const b = ev.target.closest("button[data-type]");
      if (!b) return;
      type = b.dataset.type;
      activeCity = null;
      seg.querySelectorAll("button").forEach(x => x.setAttribute("aria-pressed", String(x === b)));
      applyFilter();
      refresh();
    });
  }

  map.addEventListener("click", ev => {
    const g = ev.target.closest(".marker");
    if (g) setActive(g.dataset.city);
  });

  map.addEventListener("keydown", ev => {
    const g = ev.target.closest(".marker");
    if (g && (ev.key === "Enter" || ev.key === " ")) { ev.preventDefault(); setActive(g.dataset.city); }
  });

  /* pointing at a card lights its city on the map, and back again on the way out */
  host.addEventListener("mouseover", ev => {
    const c = ev.target.closest(".tl-card");
    if (c) highlight(c.dataset.city);
  });
  host.addEventListener("mouseout", refresh);
  host.addEventListener("focusin", ev => {
    const c = ev.target.closest(".tl-card");
    if (c) highlight(c.dataset.city);
  });

  applyFilter();

  /* The scroll position drives the map. The root is squeezed to a band across the middle of the
     viewport, so at most one item is inside it and the city under the reader's eye is the city
     that lights up. Nothing here animates: the marker's own halo is what moves, and site.css
     already gates that behind prefers-reduced-motion. */
  if ("IntersectionObserver" in window && items.length) {
    const inBand = new Set();
    const io = new IntersectionObserver(entries => {
      entries.forEach(en => {
        if (en.isIntersecting) inBand.add(en.target);
        else inBand.delete(en.target);
      });
      const current = items.find(it => inBand.has(it) && !it.hidden);
      const city = current ? current.dataset.city : null;
      if (city === scrollCity) return;
      scrollCity = city;
      if (!activeCity) { refresh(); announce(city); }
    }, { rootMargin: "-45% 0px -45% 0px" });
    items.forEach(it => io.observe(it));
  }
})();
