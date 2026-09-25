/* Stoep Barbershop — barber dashboard: login + bookings list */
(function () {
  "use strict";

  var session = { barberId: null, barberName: null, isOwner: false };

  document.addEventListener("DOMContentLoaded", function () {
    var loginForm = document.getElementById("loginForm");
    var logoutBtn = document.getElementById("logoutBtn");
    var refreshBtn = document.getElementById("refreshBtn");
    var barberFilter = document.getElementById("barberFilter");

    loginForm.addEventListener("submit", handleLogin);
    logoutBtn.addEventListener("click", handleLogout);
    refreshBtn.addEventListener("click", loadBookings);
    barberFilter.addEventListener("change", loadBookings);

    setDefaultDateRange();

    if (window.STOEP_API.isLoggedIn()) {
      showDashboard();
    }
  });

  function setDefaultDateRange() {
    var today = new Date();
    var in7 = new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000);
    document.getElementById("fromDate").value = toInputDate(today);
    document.getElementById("toDate").value = toInputDate(in7);
  }

  function toInputDate(d) {
    return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
  }

  function handleLogin(e) {
    e.preventDefault();
    var user = document.getElementById("loginUser").value.trim();
    var pass = document.getElementById("loginPass").value;
    var errorEl = document.getElementById("loginError");
    errorEl.style.display = "none";

    window.STOEP_API.login(user, pass).then(function (res) {
      session.barberId = res.barberId;
      session.barberName = res.barberName;
      session.isOwner = res.isOwner;
      showDashboard();
    }).catch(function (err) {
      errorEl.textContent = err.message;
      errorEl.style.display = "block";
    });
  }

  function handleLogout() {
    window.STOEP_API.logout();
    document.getElementById("dashApp").style.display = "none";
    document.getElementById("loginForm").style.display = "grid";
    document.getElementById("loginForm").reset();
  }

  function showDashboard() {
    document.getElementById("loginForm").style.display = "none";
    document.getElementById("dashApp").style.display = "block";
    document.getElementById("dashSubtitle").textContent = session.barberName
      ? ("Signed in as " + session.barberName + (session.isOwner ? " (owner view — all barbers)" : ""))
      : "";

    if (session.isOwner && typeof BARBERS !== "undefined") {
      var select = document.getElementById("barberFilter");
      var wrap = document.getElementById("barberFilterWrap");
      select.innerHTML = '<option value="">All barbers</option>' +
        BARBERS.map(function (b) { return '<option value="' + b.id + '">' + b.name + "</option>"; }).join("");
      wrap.style.display = "block";
    }

    loadBookings();
  }

  function loadBookings() {
    var from = document.getElementById("fromDate").value;
    var to = document.getElementById("toDate").value;
    var barberId = document.getElementById("barberFilter").value || undefined;
    var errorEl = document.getElementById("dashError");
    var emptyEl = document.getElementById("dashEmpty");
    var table = document.getElementById("dashTable");
    errorEl.style.display = "none";

    window.STOEP_API.getDashboardBookings({ from: from, to: to, barberId: barberId })
      .then(function (bookings) {
        renderTable(bookings);
      })
      .catch(function (err) {
        if (err.status === 401) { handleLogout(); return; }
        errorEl.textContent = err.message;
        errorEl.style.display = "block";
        table.style.display = "none";
        emptyEl.style.display = "none";
      });
  }

  function renderTable(bookings) {
    var table = document.getElementById("dashTable");
    var body = document.getElementById("dashTableBody");
    var emptyEl = document.getElementById("dashEmpty");

    if (!bookings.length) {
      table.style.display = "none";
      emptyEl.style.display = "block";
      return;
    }
    emptyEl.style.display = "none";
    table.style.display = "table";

    body.innerHTML = bookings.map(function (b) {
      return "<tr>" +
        "<td>" + b.date + "</td>" +
        "<td>" + minutesToLabel(b.startMinutes) + " – " + minutesToLabel(b.endMinutes) + "</td>" +
        "<td>" + escapeHtml(b.serviceName) + "<br><small>R" + b.price + "</small></td>" +
        "<td>" + escapeHtml(b.barberName) + "</td>" +
        "<td>" + escapeHtml(b.customerName) + "<br><small>" + escapeHtml(b.customerPhone) + "</small></td>" +
        "<td><span class=\"status-pill status-" + b.status + "\">" + b.status + "</span></td>" +
        "<td>" + renderActions(b) + "</td>" +
        "</tr>";
    }).join("");

    body.querySelectorAll("[data-action]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var id = btn.getAttribute("data-id");
        var status = btn.getAttribute("data-action");
        btn.disabled = true;
        window.STOEP_API.updateBookingStatus(id, status)
          .then(loadBookings)
          .catch(function (err) {
            alert(err.message);
            btn.disabled = false;
          });
      });
    });
  }

  function renderActions(b) {
    if (b.status !== "Confirmed") return "—";
    return '<div class="row-actions">' +
      '<button type="button" data-action="Completed" data-id="' + b.id + '">Mark done</button>' +
      '<button type="button" data-action="NoShow" data-id="' + b.id + '">No-show</button>' +
      '<button type="button" data-action="Cancelled" data-id="' + b.id + '">Cancel</button>' +
      "</div>";
  }

  function minutesToLabel(mins) {
    var h = Math.floor(mins / 60), m = mins % 60;
    var period = h >= 12 ? "PM" : "AM";
    var h12 = h % 12 === 0 ? 12 : h % 12;
    return h12 + ":" + String(m).padStart(2, "0") + " " + period;
  }

  function escapeHtml(str) {
    return String(str).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }
})();
