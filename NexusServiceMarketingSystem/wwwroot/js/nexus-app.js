// Nexus Communications - application shell behaviour (presentation only, no business logic).
// Sidebar, quick track search, toasts, confirm overlay and dashboard counters. Loaded after site.js.
(function () {
  'use strict';

  var doc = document.documentElement;
  var body = document.body;
  var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // ------------------------------------------------------------------ 1. Arrival of the cut overlay
  // html.nx-arrive plays the panels away; drop it afterwards so the next "leave" can run.
  if (doc.classList.contains('nx-arrive')) setTimeout(function () { doc.classList.remove('nx-arrive'); }, 1100);

  // ------------------------------------------------------------------ 2. Sidebar (off-canvas below 992px)
  var side = document.getElementById('nxSide');
  var burger = document.querySelector('[data-side-toggle]');
  function setSide(open) {
    body.classList.toggle('nx-side-open', open);
    if (burger) { burger.setAttribute('aria-expanded', open ? 'true' : 'false'); burger.setAttribute('aria-label', open ? 'Close menu' : 'Open menu'); }
  }
  if (burger) burger.addEventListener('click', function () { setSide(!body.classList.contains('nx-side-open')); });
  document.querySelectorAll('[data-side-close]').forEach(function (el) { el.addEventListener('click', function () { setSide(false); }); });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setSide(false); });

  // Highlight the sidebar link that best matches the current address (longest matching prefix).
  if (side) {
    var path = location.pathname.toLowerCase().replace(/\/index$/, '').replace(/\/$/, '');
    var best = null, bestLen = 0;
    side.querySelectorAll('.nx-side__link[href]').forEach(function (l) {
      var h = (l.getAttribute('href') || '').toLowerCase().split('?')[0].replace(/\/index$/, '').replace(/\/$/, '');
      if (!h) return;
      var match = path === h || path.indexOf(h + '/') === 0;
      if (match && h.length > bestLen) { best = l; bestLen = h.length; }
    });
    if (best) { best.classList.add('is-active'); best.setAttribute('aria-current', 'page'); }
  }

  // Pages without ViewData["Title"]: take the breadcrumb and tab title from the page heading.
  var crumb = document.querySelector('.nx-crumb strong');
  var heading = document.querySelector('main h1');
  if (crumb && !crumb.textContent.trim() && heading) {
    var title = heading.getAttribute('aria-label') || heading.textContent.replace(/\s+/g, ' ').trim();
    crumb.textContent = title;
    if (/^\s*-/.test(document.title)) document.title = title + ' ' + document.title.trim();
  }

  // ------------------------------------------------------------------ 3. Quick track search
  // Order numbers are D/B/T + 10 digits, account IDs D/B/T + 15 digits; anything else shakes the box.
  var quick = document.querySelector('[data-quick-track]');
  if (quick) {
    var input = quick.querySelector('input');
    quick.addEventListener('submit', function (e) {
      e.preventDefault();
      var v = (input.value || '').trim().toUpperCase();
      var param = /^[DBT]\d{10}$/.test(v) ? 'orderNumber' : /^[DBT]\d{15}$/.test(v) ? 'accountId' : null;
      if (!param) {
        quick.classList.remove('is-invalid'); void quick.offsetWidth; quick.classList.add('is-invalid');
        input.setCustomValidity('Enter an order number (e.g. B0000000001) or account ID (e.g. B001000000000001)');
        input.reportValidity();
        return;
      }
      input.setCustomValidity('');
      startLeave();
      var url = quick.getAttribute('action') + '?' + param + '=' + encodeURIComponent(v);
      setTimeout(function () { location.href = url; }, reduceMotion ? 0 : 520);
    });
    input.addEventListener('input', function () { input.setCustomValidity(''); quick.classList.remove('is-invalid'); });
    document.addEventListener('keydown', function (e) {
      var t = e.target.tagName;
      if (e.key === '/' && t !== 'INPUT' && t !== 'TEXTAREA' && t !== 'SELECT' && !e.target.isContentEditable) { e.preventDefault(); input.focus(); }
    });
  }

  // Shared by site.js links and the quick search: cover the page with the cut panels.
  function startLeave() {
    if (reduceMotion) return;
    try { sessionStorage.setItem('nx-cut', '1'); } catch (err) { }
    body.classList.add('nx-cutting');
    setTimeout(function () { body.classList.remove('nx-cutting'); }, 2600);
  }
  window.nxStartLeave = startLeave;

  // ------------------------------------------------------------------ 4. Toasts
  // Success messages (TempData) slide in at the top right; site.js counts them down and removes them.
  var successes = document.querySelectorAll('main .alert-success');
  if (successes.length) {
    var stack = document.createElement('div');
    stack.className = 'nx-toasts'; stack.setAttribute('role', 'status'); stack.setAttribute('aria-live', 'polite');
    body.appendChild(stack);
    successes.forEach(function (a) { stack.appendChild(a); });
  }

  // ------------------------------------------------------------------ 5. Confirm overlay
  // Replaces window.confirm() on destructive forms: inline onsubmit="return confirm('...')" and forms
  // whose submit button is a danger button (Delete, Deactivate, Suspend, Cancel order, Replace).
  var modal = null, pending = null;
  function buildModal() {
    modal = document.createElement('div');
    modal.className = 'nx-modal'; modal.setAttribute('role', 'dialog'); modal.setAttribute('aria-modal', 'true'); modal.setAttribute('aria-labelledby', 'nxModalTitle');
    modal.innerHTML =
      '<div class="nx-modal__scrim" data-no></div>' +
      '<div class="nx-modal__card">' +
      '<div class="nx-modal__icon"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3 2 20.5h20zM12 10v4.5M12 17.5v.5"/></svg></div>' +
      '<h3 id="nxModalTitle">Are you sure?</h3><p></p>' +
      '<div class="nx-modal__btns"><button type="button" class="btn btn-outline-secondary" data-no>Go back</button><button type="button" class="btn btn-danger" data-yes>Yes, continue</button></div>' +
      '</div>';
    body.appendChild(modal);
    modal.querySelectorAll('[data-no]').forEach(function (b) { b.addEventListener('click', closeModal); });
    modal.querySelector('[data-yes]').addEventListener('click', function () {
      var p = pending; closeModal();
      if (!p) return;
      p.form.setAttribute('data-confirmed', '1');
      if (p.form.requestSubmit) p.form.requestSubmit(p.submitter || undefined); else p.form.submit();
    });
    modal.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeModal(); });
  }
  function openModal(form, submitter, message, action) {
    if (!modal) buildModal();
    pending = { form: form, submitter: submitter };
    modal.querySelector('p').textContent = message;
    modal.querySelector('[data-yes]').textContent = action ? 'Yes, ' + action.toLowerCase() : 'Yes, continue';
    modal.classList.add('is-open');
    setTimeout(function () { modal.querySelector('[data-yes]').focus(); }, 50);
  }
  function closeModal() { if (modal) modal.classList.remove('is-open'); pending = null; }

  document.querySelectorAll('form[onsubmit*="confirm("]').forEach(function (f) {
    var m = /confirm\((['"])(.*?)\1\)/.exec(f.getAttribute('onsubmit') || '');
    f.setAttribute('data-confirm', m ? m[2] : 'Do you want to continue?');
    f.removeAttribute('onsubmit');
  });
  document.querySelectorAll('form').forEach(function (f) {
    if (f.hasAttribute('data-confirm')) return;
    var danger = f.querySelector('button.btn-danger, button.btn-outline-danger');
    if (danger && (f.getAttribute('method') || '').toLowerCase() === 'post') {
      var label = danger.textContent.trim();
      f.setAttribute('data-confirm', 'This will ' + label.toLowerCase() + ' the selected record. You can’t undo this from here.');
    }
  });
  document.addEventListener('submit', function (e) {
    var f = e.target;
    if (!f.hasAttribute || !f.hasAttribute('data-confirm')) return;
    if (f.getAttribute('data-confirmed') === '1') { f.removeAttribute('data-confirmed'); return; }
    e.preventDefault(); e.stopImmediatePropagation();
    var btn = e.submitter || f.querySelector('button[type=submit], button:not([type])');
    openModal(f, e.submitter, f.getAttribute('data-confirm'), btn ? btn.textContent.trim() : '');
  }, true);

  // ------------------------------------------------------------------ 6. Status badges
  // Table cells that hold only a status word become a coloured badge (one colour system everywhere).
  var tones = {
    placed: 'idle', pending: 'idle', draft: 'idle', cancelled: 'bad',
    underfeasibilitycheck: 'info', issued: 'info', inprogress: 'info',
    feasible: 'warn', partiallypaid: 'warn', temporarilyinactive: 'warn', unpaid: 'warn', low: 'bad',
    connected: 'ok', active: 'ok', paid: 'ok', available: 'ok',
    notfeasible: 'bad', permanentlyinactive: 'bad', overdue: 'bad', inactive: 'idle'
  };
  function human(word) { return word.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, function (c) { return c.toUpperCase(); }).replace(/ ([A-Z])/g, function (m, c) { return ' ' + c.toLowerCase(); }); }
  document.querySelectorAll('main td').forEach(function (td) {
    if (td.children.length) return;
    var text = td.textContent.trim(), key = text.replace(/[\s_-]/g, '').toLowerCase();
    if (!tones[key]) return;
    td.innerHTML = '';
    var b = document.createElement('span');
    b.className = 'nx-badge ' + tones[key];
    b.textContent = human(text);
    td.appendChild(b);
  });

  // ------------------------------------------------------------------ 7. Dashboard counters
  // [data-kpi] values count up with thousands separators once visible.
  var kpis = document.querySelectorAll('[data-kpi]');
  function fmt(n, dec) { return n.toLocaleString('en-US', { minimumFractionDigits: dec, maximumFractionDigits: dec }); }
  if (kpis.length) {
    var run = function (el) {
      var end = parseFloat(el.getAttribute('data-kpi')) || 0, dec = parseInt(el.getAttribute('data-dec') || '0', 10);
      var target = el.querySelector('span') || el;
      if (reduceMotion || end === 0) { target.textContent = fmt(end, dec); return; }
      var t0 = performance.now(), dur = 1300;
      (function step(now) {
        var k = Math.min(1, (now - t0) / dur), eased = 1 - Math.pow(1 - k, 4);
        target.textContent = fmt(end * eased, dec);
        if (k < 1) requestAnimationFrame(step);
      })(t0);
    };
    if ('IntersectionObserver' in window) {
      var kio = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) { if (e.isIntersecting) { kio.unobserve(e.target); run(e.target); } });
      }, { threshold: 0.4 });
      kpis.forEach(function (k) { kio.observe(k); });
    } else kpis.forEach(run);
  }
})();
