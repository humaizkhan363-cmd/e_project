// Nexus Communications - small UI helpers (animations only, no business logic)
(function () {
  'use strict';

  // 1. Scroll reveal for panels, tiles and tables
  var targets = document.querySelectorAll('main .nx-panel, main .nx-tile, main .table, main .card');
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('is-visible'); io.unobserve(e.target); }
      });
    }, { threshold: 0.08 });
    targets.forEach(function (el) { el.classList.add('nx-reveal'); io.observe(el); });
  }

  // 2. Wrap wide tables so they scroll on phones
  document.querySelectorAll('main table.table').forEach(function (t) {
    if (!t.parentElement.classList.contains('table-responsive')) {
      var w = document.createElement('div');
      w.className = 'table-responsive';
      t.parentNode.insertBefore(w, t);
      w.appendChild(t);
    }
  });

  // 3. Auto-hide success alerts after 5 seconds
  document.querySelectorAll('.alert-success').forEach(function (a) {
    setTimeout(function () {
      a.classList.add('nx-fade-out');
      setTimeout(function () { a.remove(); }, 600);
    }, 5000);
  });

  // 4. Highlight the active navigation link
  var path = location.pathname.toLowerCase();
  document.querySelectorAll('.nx-navbar .navbar-nav .nav-link[href]').forEach(function (l) {
    var h = (l.getAttribute('href') || '').toLowerCase();
    if (h && h !== '/' && h !== '#' && path.indexOf(h) === 0) { l.style.color = '#5eead4'; l.style.fontWeight = '600'; }
  });

  // 5. Prevent double submit: disable button briefly after submit
  document.querySelectorAll('form').forEach(function (f) {
    f.addEventListener('submit', function () {
      if (f.querySelector('.input-validation-error')) return;
      var b = f.querySelector('button[type=submit], button:not([type])');
      if (b) { setTimeout(function () { b.disabled = true; }, 0); setTimeout(function () { b.disabled = false; }, 4000); }
    });
  });
})();
