// Nexus Communications - motion and micro-interaction layer (presentation only, no business logic).
// Every effect uses transform / opacity, runs in requestAnimationFrame where it follows the pointer or
// scroll, and is skipped when the user prefers reduced motion or (for hover effects) has no fine pointer.
(function () {
  'use strict';

  var doc = document.documentElement;
  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

  // ------------------------------------------------------------------ 1. Loader
  // The loader is only shown on the first page of a visit (html.nx-first is set in <head>).
  // It hides on window load, after a short minimum so the animation reads, with a safety timeout.
  var started = Date.now();
  function hideLoader() {
    var wait = Math.max(0, 700 - (Date.now() - started));
    setTimeout(function () { doc.classList.add('nx-loaded'); }, wait);
  }
  if (doc.classList.contains('nx-first')) {
    if (document.readyState === 'complete') hideLoader(); else window.addEventListener('load', hideLoader);
    setTimeout(function () { doc.classList.add('nx-loaded'); }, 2500);
  }

  // ------------------------------------------------------------------ 2. Page leave transition
  // Internal links fade the page out and run the top progress bar before navigating.
  document.addEventListener('click', function (e) {
    var a = e.target.closest('a[href]');
    if (!a || reduceMotion || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    var href = a.getAttribute('href');
    if (!href || href.charAt(0) === '#' || a.target === '_blank' || a.hasAttribute('download') || a.hasAttribute('data-bs-toggle')) return;
    var url = new URL(a.href, location.href);
    if (url.origin !== location.origin || (url.pathname === location.pathname && url.search === location.search && url.hash)) return;
    e.preventDefault();
    document.body.classList.add('nx-leaving');
    setTimeout(function () { location.href = a.href; }, 200);
    // A file download never unloads the page: bring it back if we are still here.
    setTimeout(function () { document.body.classList.remove('nx-leaving'); }, 1800);
  });
  // Coming back with the browser's back button restores the page from cache: undo the leave state.
  window.addEventListener('pageshow', function () { document.body.classList.remove('nx-leaving'); });

  // ------------------------------------------------------------------ 3. Scroll: progress, navbar, back-to-top
  var ticking = false;
  function onScroll() {
    var max = doc.scrollHeight - innerHeight;
    var p = max > 0 ? Math.min(1, scrollY / max) : 0;
    doc.style.setProperty('--nx-scroll', p.toFixed(4));
    document.body.classList.toggle('nx-scrolled', scrollY > 12);
    document.body.classList.toggle('nx-scrolled-far', scrollY > 480);
    parallaxScroll();
    ticking = false;
  }
  window.addEventListener('scroll', function () { if (!ticking) { ticking = true; requestAnimationFrame(onScroll); } }, { passive: true });

  var topBtn = document.querySelector('.nx-top');
  if (topBtn) topBtn.addEventListener('click', function () { window.scrollTo({ top: 0, behavior: reduceMotion ? 'auto' : 'smooth' }); });

  // Scroll parallax: [data-parallax="0.2"] moves at a fraction of the scroll speed (small deltas only).
  var parallaxEls = reduceMotion ? [] : Array.prototype.slice.call(document.querySelectorAll('[data-parallax]'));
  function parallaxScroll() {
    for (var i = 0; i < parallaxEls.length; i++) {
      var el = parallaxEls[i];
      var speed = parseFloat(el.getAttribute('data-parallax')) || 0.15;
      var rect = el.parentElement.getBoundingClientRect();
      if (rect.bottom < 0 || rect.top > innerHeight) continue;
      el.style.transform = 'translate3d(0,' + (-rect.top * speed).toFixed(1) + 'px,0)';
    }
  }
  onScroll();

  // ------------------------------------------------------------------ 4. Stage: pointer parallax + spotlight
  // Sets --mx/--my (-1..1) used by .nx-layer and the 3D globe, and --px/--py for the spotlight.
  if (!reduceMotion && finePointer) {
    document.querySelectorAll('.nx-stage').forEach(function (stage) {
      var raf = 0, ev = null;
      stage.addEventListener('pointermove', function (e) {
        ev = e;
        if (raf) return;
        raf = requestAnimationFrame(function () {
          var r = stage.getBoundingClientRect();
          var x = (ev.clientX - r.left) / r.width, y = (ev.clientY - r.top) / r.height;
          stage.style.setProperty('--mx', ((x - 0.5) * 2).toFixed(3));
          stage.style.setProperty('--my', ((y - 0.5) * 2).toFixed(3));
          stage.style.setProperty('--px', (x * 100).toFixed(1) + '%');
          stage.style.setProperty('--py', (y * 100).toFixed(1) + '%');
          raf = 0;
        });
      });
      stage.addEventListener('pointerleave', function () { stage.style.setProperty('--mx', 0); stage.style.setProperty('--my', 0); });
    });
  }

  // ------------------------------------------------------------------ 4b. Page structure enhancements
  // a) Inner pages: the first h1 (and the intro paragraph right after it) becomes a title bar with a breadcrumb.
  var main = document.querySelector('main');
  var firstH1 = main && main.querySelector('h1');
  if (firstH1 && !firstH1.closest('.nx-stage, .nx-auth, .nx-titlebar')) {
    var bar = document.createElement('header');
    bar.className = 'nx-titlebar';
    var crumbs = document.createElement('ol');
    crumbs.className = 'nx-crumbs';
    crumbs.setAttribute('aria-label', 'Breadcrumb');
    var addCrumb = function (text, href) {
      var li = document.createElement('li');
      if (href) { var a = document.createElement('a'); a.href = href; a.textContent = text; li.appendChild(a); }
      else { li.textContent = text; li.setAttribute('aria-current', 'page'); }
      crumbs.appendChild(li);
    };
    addCrumb('Home', '/');
    var dash = Array.prototype.find.call(document.querySelectorAll('.nx-navbar .nav-link'), function (l) { return l.textContent.trim() === 'My Dashboard'; });
    if (dash && location.pathname.toLowerCase().indexOf(dash.getAttribute('href').toLowerCase()) === 0 && location.pathname.toLowerCase() !== dash.getAttribute('href').toLowerCase()) {
      addCrumb('Dashboard', dash.getAttribute('href'));
    }
    addCrumb(firstH1.textContent.trim());
    // When the heading shares a flex row with action buttons, the whole row moves into the bar.
    var row = firstH1.parentElement !== main && firstH1.parentElement.classList.contains('d-flex') ? firstH1.parentElement : null;
    var head = row || firstH1;
    var intro = head.nextElementSibling;
    head.parentNode.insertBefore(bar, head);
    bar.appendChild(crumbs);
    if (row) { row.classList.add('nx-titlebar__actions', 'align-items-center'); row.style.marginBottom = '0'; }
    bar.appendChild(head);
    if (intro && intro.tagName === 'P' && intro.classList.contains('text-muted') && !intro.querySelector('a.btn, form')) {
      intro.classList.remove('text-muted');
      intro.classList.add('nx-titlebar__intro');
      bar.appendChild(intro);
    }
  }

  // b) Multi-field forms are shown as cards.
  document.querySelectorAll('main form').forEach(function (f) {
    if (f.closest('table, nav, .nx-panel, .nx-stage, .nx-auth, .alert, .nx-formcard, .card, .nx-titlebar') || f.classList.contains('d-inline')) return;
    var fields = f.querySelectorAll('input:not([type=hidden]):not([type=checkbox]), select, textarea');
    if (fields.length >= 2) f.classList.add('nx-formcard');
  });

  // c) Status values become coloured badges; service types get their icon. Only cells whose whole text is the value.
  var statusMap = {
    'paid': ['ok', 'Paid'], 'partially paid': ['warn', 'Partially paid'], 'partiallypaid': ['warn', 'Partially paid'], 'unpaid': ['warn', 'Unpaid'],
    'overdue': ['bad', 'Overdue', true], 'issued': ['info', 'Issued'], 'cancelled': ['muted', 'Cancelled'],
    'active': ['ok', 'Active'], 'inactive': ['muted', 'Inactive'], 'temporarilyinactive': ['warn', 'Temporarily inactive'], 'permanentlyinactive': ['muted', 'Permanently inactive'],
    'placed': ['info', 'Placed'], 'underfeasibilitycheck': ['warn', 'Under feasibility check'], 'feasible': ['ok', 'Feasible'], 'notfeasible': ['bad', 'Not feasible'],
    'connected': ['ok', 'Connected'], 'pending': ['warn', 'Pending']
  };
  var typeIcons = {
    'dialup': ['Dial-Up', '<path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/>'],
    'broadband': ['Broadband', '<rect x="2" y="14" width="20" height="7" rx="2"/><path d="M6 18h.01M10 18h.01M15 10a4 4 0 0 0-6 0M18 7a8.5 8.5 0 0 0-12 0"/>'],
    'telephone': ['Telephone', '<path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.4c.9.3 1.9.6 2.8.7a2 2 0 0 1 1.7 2z"/>']
  };
  document.querySelectorAll('main td, main dd, main strong').forEach(function (el) {
    if (el.children.length || el.closest('.nx-stage')) return;
    var key = el.textContent.trim().toLowerCase();
    var st = statusMap[key];
    if (st) {
      var b = document.createElement('span');
      b.className = 'nx-status nx-status--' + st[0] + (st[2] ? ' nx-status--pulse' : '');
      b.textContent = st[1];
      el.textContent = '';
      el.appendChild(b);
      return;
    }
    var ty = typeIcons[key];
    if (ty && el.tagName === 'TD') {
      el.innerHTML = '<span class="nx-type"><svg class="nx-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + ty[1] + '</svg>' + ty[0] + '</span>';
    }
  });

  // ------------------------------------------------------------------ 5. Headline split reveal
  // Wraps each word of [data-split] headings so they rise in one after another (short headlines only).
  document.querySelectorAll('[data-split]').forEach(function (h) {
    if (reduceMotion) return;
    var i = 0;
    (function wrap(node) {
      Array.prototype.slice.call(node.childNodes).forEach(function (child) {
        if (child.nodeType === 3) {
          var frag = document.createDocumentFragment();
          child.textContent.split(/(\s+)/).forEach(function (part) {
            if (!part) return;
            if (/^\s+$/.test(part)) { frag.appendChild(document.createTextNode(part)); return; }
            var outer = document.createElement('span'); outer.className = 'nx-w';
            var inner = document.createElement('span'); inner.textContent = part; inner.style.setProperty('--i', i++);
            outer.appendChild(inner); frag.appendChild(outer);
          });
          node.replaceChild(frag, child);
        } else if (child.nodeType === 1 && child.tagName !== 'BR') {
          wrap(child);
        }
      });
    })(h);
    h.setAttribute('aria-label', h.textContent.replace(/\s+/g, ' ').trim());
    h.classList.add('nx-split');
  });

  // ------------------------------------------------------------------ 6. Scroll reveal with stagger
  // Explicit [data-reveal] elements plus the common building blocks; siblings in one group stagger.
  var revealSel = '[data-reveal], main .nx-tile, main .nx-panel, main .card, main h2, main .table-responsive, main .nx-formcard, main .nx-kpi, main .nx-plan, main .alert-secondary';
  var revealEls = Array.prototype.slice.call(document.querySelectorAll(revealSel)).filter(function (el) { return !el.closest('.nx-stage') && !el.closest('.nx-reveal'); });
  if ('IntersectionObserver' in window && !reduceMotion) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        // Reveal when on screen, and also anything already scrolled past (anchor jumps, fast scrolling).
        if (!e.isIntersecting && e.boundingClientRect.bottom > 0) return;
        var el = e.target;
        el.classList.add('is-visible');
        io.unobserve(el);
        // Once revealed, drop the stagger delay so later hover/tilt transitions respond at once.
        setTimeout(function () { el.style.setProperty('--stagger', 0); }, 900 + (parseInt(el.style.getPropertyValue('--stagger'), 10) || 0) * 80);
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
    revealEls.forEach(function (el) {
      var group = el.parentElement && el.parentElement.closest('.row') ? el.closest('[class*="col"]') || el : el;
      var idx = group.parentElement ? Array.prototype.indexOf.call(group.parentElement.children, group) : 0;
      el.style.setProperty('--stagger', Math.min(idx, 8));
      el.classList.add('nx-reveal');
      io.observe(el);
    });
  }

  // Printing shows everything at once.
  window.addEventListener('beforeprint', function () {
    document.querySelectorAll('.nx-reveal').forEach(function (el) { el.classList.add('is-visible'); });
  });

  // Table rows cascade in when their table is first shown.
  document.querySelectorAll('main table.table').forEach(function (t) {
    // Wrap wide tables so they scroll sideways on phones.
    if (!t.parentElement.classList.contains('table-responsive')) {
      var w = document.createElement('div'); w.className = 'table-responsive';
      t.parentNode.insertBefore(w, t); w.appendChild(t);
    }
    if (reduceMotion) return;
    var rows = t.tBodies[0] ? t.tBodies[0].rows : [];
    for (var r = 0; r < rows.length && r < 25; r++) { rows[r].style.setProperty('--stagger', r); rows[r].classList.add('nx-row-in'); }
  });

  // ------------------------------------------------------------------ 7. 3D tilt with glare
  // Tiles and [data-tilt] cards rotate toward the pointer (max 7deg); the glare follows the pointer.
  document.querySelectorAll('.nx-tile, .nx-feature').forEach(function (el) { el.setAttribute('data-tilt', ''); });
  if (!reduceMotion && finePointer) {
    document.querySelectorAll('[data-tilt]').forEach(function (el) {
      var max = parseFloat(el.getAttribute('data-tilt')) || 7, raf = 0, ev = null;
      el.addEventListener('pointerenter', function () { el.classList.add('is-tilting'); });
      el.addEventListener('pointermove', function (e) {
        ev = e;
        if (raf) return;
        raf = requestAnimationFrame(function () {
          var r = el.getBoundingClientRect();
          var x = (ev.clientX - r.left) / r.width, y = (ev.clientY - r.top) / r.height;
          el.style.setProperty('--ry', ((x - 0.5) * 2 * max).toFixed(2) + 'deg');
          el.style.setProperty('--rx', ((0.5 - y) * 2 * max).toFixed(2) + 'deg');
          el.style.setProperty('--gx', (x * 100).toFixed(1) + '%');
          el.style.setProperty('--gy', (y * 100).toFixed(1) + '%');
          raf = 0;
        });
      });
      el.addEventListener('pointerleave', function () {
        el.classList.remove('is-tilting');
        el.style.setProperty('--rx', '0deg'); el.style.setProperty('--ry', '0deg');
      });
    });
  } else {
    document.querySelectorAll('[data-tilt]').forEach(function (el) { el.removeAttribute('data-tilt'); });
  }

  // ------------------------------------------------------------------ 8. Magnetic buttons
  if (!reduceMotion && finePointer) {
    document.querySelectorAll('.nx-magnetic').forEach(function (b) {
      b.addEventListener('pointermove', function (e) {
        var r = b.getBoundingClientRect();
        b.style.setProperty('--tx', ((e.clientX - r.left - r.width / 2) * 0.18).toFixed(1) + 'px');
        b.style.setProperty('--ty', ((e.clientY - r.top - r.height / 2) * 0.25).toFixed(1) + 'px');
      });
      b.addEventListener('pointerleave', function () { b.style.setProperty('--tx', '0px'); b.style.setProperty('--ty', '0px'); });
    });
  }

  // ------------------------------------------------------------------ 9. Ripple on buttons
  document.addEventListener('pointerdown', function (e) {
    var b = e.target.closest('.btn');
    if (!b || reduceMotion || b.classList.contains('btn-link')) return;
    var r = b.getBoundingClientRect(), size = Math.max(r.width, r.height) * 2.2;
    var s = document.createElement('span');
    s.className = 'nx-ripple';
    s.style.width = s.style.height = size + 'px';
    s.style.left = (e.clientX - r.left - size / 2) + 'px';
    s.style.top = (e.clientY - r.top - size / 2) + 'px';
    b.appendChild(s);
    setTimeout(function () { s.remove(); }, 650);
  });

  // Glass cards: a soft light flash spreads from the click point.
  document.addEventListener('pointerdown', function (e) {
    var c = e.target.closest('.nx-tile, .nx-plan, .nx-city, .nx-pill, .nx-why, .nx-kpi, a.card');
    if (!c || reduceMotion || e.target.closest('.btn')) return;
    var r = c.getBoundingClientRect(), f = document.createElement('span');
    f.className = 'nx-glass-flash';
    f.style.setProperty('--cx', (e.clientX - r.left) + 'px');
    f.style.setProperty('--cy', (e.clientY - r.top) + 'px');
    c.appendChild(f);
    setTimeout(function () { f.remove(); }, 750);
  });

  // ------------------------------------------------------------------ 10. Forms
  // a) Valid submit shows a spinner on the button and prevents a double submit.
  document.querySelectorAll('form').forEach(function (f) {
    f.addEventListener('submit', function (e) {
      var $ = window.jQuery;
      if (e.defaultPrevented || ($ && $(f).data('validator') && !$(f).valid())) return;
      var b = (e.submitter && e.submitter.classList.contains('btn')) ? e.submitter : f.querySelector('button[type=submit].btn, button:not([type]).btn');
      if (!b || (f.getAttribute('method') || 'get').toLowerCase() === 'get') return;
      setTimeout(function () { b.classList.add('is-loading'); b.setAttribute('aria-busy', 'true'); }, 0);
      setTimeout(function () { b.classList.remove('is-loading'); b.removeAttribute('aria-busy'); }, 8000);
    });
  });
  window.addEventListener('pageshow', function () { document.querySelectorAll('.btn.is-loading').forEach(function (b) { b.classList.remove('is-loading'); }); });

  // b) Show / hide password.
  var eye = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>';
  var eyeOff = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M17.9 17.9A10.4 10.4 0 0 1 12 19c-6.5 0-10-7-10-7a18.5 18.5 0 0 1 4.1-5.1M9.9 4.2A9.6 9.6 0 0 1 12 4c6.5 0 10 7 10 7a18.6 18.6 0 0 1-2.2 3.2M1 1l22 22"/></svg>';
  document.querySelectorAll('input[type=password]').forEach(function (inp) {
    var wrap = document.createElement('div'); wrap.className = 'nx-pass';
    inp.parentNode.insertBefore(wrap, inp); wrap.appendChild(inp);
    var t = document.createElement('button');
    t.type = 'button'; t.className = 'nx-pass__toggle'; t.innerHTML = eye; t.setAttribute('aria-label', 'Show password');
    t.addEventListener('click', function () {
      var show = inp.type === 'password';
      inp.type = show ? 'text' : 'password';
      t.innerHTML = show ? eyeOff : eye;
      t.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
    });
    wrap.appendChild(t);
  });

  // ------------------------------------------------------------------ 11. Alerts: countdown, then close
  document.querySelectorAll('.alert-success').forEach(function (a) {
    var bar = document.createElement('span'); bar.className = 'nx-countdown'; a.appendChild(bar);
    var paused = false, left = 5000, last = Date.now();
    a.addEventListener('mouseenter', function () { paused = true; left -= Date.now() - last; });
    a.addEventListener('mouseleave', function () { paused = false; last = Date.now(); });
    (function tick() {
      if (!paused && Date.now() - last >= left) {
        a.classList.add('nx-fade-out');
        setTimeout(function () { a.remove(); }, 500);
        return;
      }
      setTimeout(tick, 200);
    })();
  });

  // ------------------------------------------------------------------ 12. Active navigation link
  var path = location.pathname.toLowerCase().replace(/\/index$/, '');
  var best = null, bestLen = 0;
  document.querySelectorAll('.nx-navbar .navbar-nav .nav-link[href]').forEach(function (l) {
    var h = (l.getAttribute('href') || '').toLowerCase().replace(/\/index$/, '');
    if (h && h !== '/' && h !== '#' && path.indexOf(h) === 0 && h.length > bestLen) { best = l; bestLen = h.length; }
  });
  if (best) { best.classList.add('is-active'); best.setAttribute('aria-current', 'page'); }

  // ------------------------------------------------------------------ 13. Count-up numbers
  // [data-count] elements count from 0 to their value when they scroll into view.
  var counters = document.querySelectorAll('[data-count]');
  if (counters.length && 'IntersectionObserver' in window) {
    var cio = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (!e.isIntersecting) return;
        cio.unobserve(e.target);
        var el = e.target, end = parseFloat(el.getAttribute('data-count')) || 0, suffix = el.getAttribute('data-suffix') || '';
        if (reduceMotion) { el.textContent = end + suffix; return; }
        var t0 = performance.now(), dur = 1400;
        (function step(now) {
          var k = Math.min(1, (now - t0) / dur), eased = 1 - Math.pow(1 - k, 4);
          el.textContent = Math.round(end * eased) + suffix;
          if (k < 1) requestAnimationFrame(step);
        })(t0);
      });
    }, { threshold: 0.6 });
    counters.forEach(function (c) { cio.observe(c); });
  }

  // ------------------------------------------------------------------ 14. Click to copy IDs
  // Order numbers (D/B/T + 10 digits) and account IDs (D/B/T + 15 digits) in tables and codes copy on click.
  var idPattern = /^[DBT](\d{10}|\d{15})$/;
  document.querySelectorAll('main td, main code, main strong, main [data-copy]').forEach(function (el) {
    var text = el.textContent.trim();
    if (el.children.length || !idPattern.test(text)) return;
    el.classList.add('nx-copy'); el.title = 'Click to copy'; el.setAttribute('data-copied', 'Copied');
    el.addEventListener('click', function () {
      if (!navigator.clipboard) return;
      navigator.clipboard.writeText(text).then(function () {
        el.classList.add('is-copied');
        setTimeout(function () { el.classList.remove('is-copied'); }, 1200);
      });
    });
  });
  // ------------------------------------------------------------------ 15. Background particle network
  // Slow drifting dots joined by faint lines (a "network" fitting a telecom brand). Dots near the pointer
  // link to it. Pauses while the tab is hidden; never runs for reduced motion.
  var canvas = document.querySelector('.nx-particles');
  if (canvas && canvas.getContext && !reduceMotion) {
    var ctx = canvas.getContext('2d'), dots = [], w = 0, h = 0, dpr = 1, mouse = null, running = true;
    var resize = function () {
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = canvas.clientWidth; h = canvas.clientHeight;
      canvas.width = w * dpr; canvas.height = h * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      var target = Math.round(Math.min(80, (w * h) / 16000));
      while (dots.length < target) dots.push({ x: Math.random() * w, y: Math.random() * h, vx: (Math.random() - .5) * .25, vy: (Math.random() - .5) * .25, r: Math.random() * 1.6 + .6 });
      dots.length = target;
    };
    resize();
    window.addEventListener('resize', resize);
    window.addEventListener('pointermove', function (e) { mouse = { x: e.clientX, y: e.clientY }; }, { passive: true });
    document.addEventListener('visibilitychange', function () { running = !document.hidden; if (running) requestAnimationFrame(frame); });
    var LINK = 130;
    var frame = function () {
      if (!running) return;
      ctx.clearRect(0, 0, w, h);
      for (var i = 0; i < dots.length; i++) {
        var d = dots[i];
        d.x += d.vx; d.y += d.vy;
        if (d.x < -10) d.x = w + 10; if (d.x > w + 10) d.x = -10;
        if (d.y < -10) d.y = h + 10; if (d.y > h + 10) d.y = -10;
        for (var j = i + 1; j < dots.length; j++) {
          var e2 = dots[j], dx = d.x - e2.x, dy = d.y - e2.y, dist = dx * dx + dy * dy;
          if (dist < LINK * LINK) {
            ctx.strokeStyle = 'rgba(125, 211, 252,' + (0.16 * (1 - Math.sqrt(dist) / LINK)).toFixed(3) + ')';
            ctx.lineWidth = 1;
            ctx.beginPath(); ctx.moveTo(d.x, d.y); ctx.lineTo(e2.x, e2.y); ctx.stroke();
          }
        }
        if (mouse) {
          var mx = d.x - mouse.x, my = d.y - mouse.y, md = mx * mx + my * my;
          if (md < 180 * 180) {
            ctx.strokeStyle = 'rgba(110, 231, 183,' + (0.28 * (1 - Math.sqrt(md) / 180)).toFixed(3) + ')';
            ctx.beginPath(); ctx.moveTo(d.x, d.y); ctx.lineTo(mouse.x, mouse.y); ctx.stroke();
          }
        }
        ctx.fillStyle = 'rgba(186, 230, 253, .7)';
        ctx.beginPath(); ctx.arc(d.x, d.y, d.r, 0, Math.PI * 2); ctx.fill();
      }
      requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
  }
})();
