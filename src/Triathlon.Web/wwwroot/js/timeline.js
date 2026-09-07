/* ============================================================
   Season timeline — the only behaviour the page needs.

   Every item, month heading and map marker is already in the DOM,
   rendered by the server in one culture. This file never builds
   markup and never touches innerHTML: it toggles classes and the
   `hidden` property on elements that are already there, which is
   what keeps the page working without script and the CSP at
   script-src 'self'.
   ============================================================ */

(function () {
  "use strict";

  const host = document.getElementById("timelineHost");
  const map = document.getElementById("ksaMap");
  if (!host || !map) return;

  const seg = document.getElementById("typeSeg");
  const items = Array.from(host.querySelectorAll(".tl-item"));
  const months = Array.from(host.querySelectorAll(".tl-month"));
  const markers = Array.from(map.querySelectorAll(".marker"));
  let type = "all", activeCity = null;

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

  function setActive(city) {
    activeCity = activeCity === city ? null : city;
    highlight(activeCity);
    if (activeCity) {
      const first = host.querySelector('.tl-item[data-city="' + activeCity + '"]:not([hidden])');
      if (first) first.scrollIntoView({ behavior: "smooth", block: "center" });
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
      highlight(null);
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
  host.addEventListener("mouseout", () => highlight(activeCity));
  host.addEventListener("focusin", ev => {
    const c = ev.target.closest(".tl-card");
    if (c) highlight(c.dataset.city);
  });

  applyFilter();
})();
