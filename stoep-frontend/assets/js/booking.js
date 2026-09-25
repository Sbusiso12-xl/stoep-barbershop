/* Stoep Barbershop — booking wizard, availability, validation, calendar generation */
(function () {
  "use strict";

  var BOOKINGS_KEY = "stoep_bookings_v1";
  var SLOT_INTERVAL = 15; // minutes
  var MIN_NOTICE_MINUTES = 30; // can't book sooner than this from "now"

  var state = {
    step: 0,
    steps: ["service", "barber", "datetime", "details"],
    service: null,
    barber: null,
    date: null,      // "YYYY-MM-DD"
    startMinutes: null,
    name: "",
    email: "",
    phone: "",
    notes: ""
  };

  document.addEventListener("DOMContentLoaded", function () {
    var root = document.getElementById("bookingApp");
    if (!root) return;

    preselectFromQuery();
    renderServiceStep();
    renderBarberStep();
    bindDateInput();
    renderStepIndicator();
    goToStep(0);

    root.querySelectorAll("[data-nav='next']").forEach(function (btn) {
      btn.addEventListener("click", handleNext);
    });
    root.querySelectorAll("[data-nav='back']").forEach(function (btn) {
      btn.addEventListener("click", handleBack);
    });

    var form = document.getElementById("bookingDetailsForm");
    if (form) form.addEventListener("submit", handleSubmit);
  });

  function preselectFromQuery() {
    var params = new URLSearchParams(window.location.search);
    var serviceId = params.get("service");
    if (serviceId && SERVICES.some(function (s) { return s.id === serviceId; })) {
      state.service = serviceId;
    }
  }

  /* ---------------- Step indicator ---------------- */
  function renderStepIndicator() {
    var wrap = document.getElementById("bookingSteps");
    if (!wrap) return;
    var labels = ["Service", "Barber", "Date & time", "Your details"];
    wrap.innerHTML = labels.map(function (label, i) {
      return '<div class="booking-step-pill" data-step-pill="' + i + '"><span class="num">' + (i + 1) + '</span>' + label + "</div>";
    }).join("");
  }

  function updateStepIndicator() {
    document.querySelectorAll("[data-step-pill]").forEach(function (el) {
      var i = Number(el.getAttribute("data-step-pill"));
      el.classList.toggle("is-active", i === state.step);
      el.classList.toggle("is-done", i < state.step);
    });
  }

  function goToStep(index) {
    state.step = index;
    document.querySelectorAll(".booking-panel").forEach(function (panel, i) {
      panel.classList.toggle("is-active", i === index);
    });
    updateStepIndicator();
    if (state.steps[index] === "datetime") renderDateTimeStep();
    if (state.steps[index] === "details") renderSummary();
    var panel = document.querySelectorAll(".booking-panel")[index];
    if (panel) panel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function handleNext() {
    var current = state.steps[state.step];
    var error = validateStep(current);
    var msg = document.getElementById("stepError");
    if (error) {
      if (msg) { msg.textContent = error; msg.style.display = "block"; }
      return;
    }
    if (msg) msg.style.display = "none";
    if (state.step < state.steps.length - 1) goToStep(state.step + 1);
  }

  function handleBack() {
    if (state.step > 0) goToStep(state.step - 1);
  }

  function validateStep(name) {
    if (name === "service" && !state.service) return "Choose a service to continue.";
    if (name === "barber" && !state.barber) return "Choose a barber to continue.";
    if (name === "datetime") {
      if (!state.date) return "Choose a date to continue.";
      if (state.startMinutes === null) return "Choose an available time to continue.";
    }
    return null;
  }

  /* ---------------- Step 1: Service ---------------- */
  function renderServiceStep() {
    var wrap = document.getElementById("serviceChoices");
    if (!wrap) return;
    wrap.innerHTML = SERVICES.map(function (s) {
      return (
        '<button type="button" class="choice-card" data-service="' + s.id + '" aria-pressed="' + (state.service === s.id) + '">' +
        "<h3>" + s.name + "</h3>" +
        '<div class="meta">R' + s.price + " · " + s.duration + " min</div>" +
        "<p>" + s.description + "</p>" +
        "</button>"
      );
    }).join("");
    if (state.service) markSelected(wrap, "service", state.service);

    wrap.addEventListener("click", function (e) {
      var card = e.target.closest("[data-service]");
      if (!card) return;
      state.service = card.getAttribute("data-service");
      markSelected(wrap, "service", state.service);
      state.startMinutes = null; // duration changed, times may no longer be valid
    });
  }

  /* ---------------- Step 2: Barber ---------------- */
  function renderBarberStep() {
    var wrap = document.getElementById("barberChoices");
    if (!wrap) return;
    wrap.innerHTML = BARBERS.map(function (b) {
      return (
        '<button type="button" class="choice-card" data-barber="' + b.id + '" aria-pressed="' + (state.barber === b.id) + '">' +
        "<h3>" + b.name + "</h3>" +
        '<div class="meta">' + b.role + "</div>" +
        "<p>Specialty: " + b.specialty + "</p>" +
        "</button>"
      );
    }).join("");
    if (state.barber) markSelected(wrap, "barber", state.barber);

    wrap.addEventListener("click", function (e) {
      var card = e.target.closest("[data-barber]");
      if (!card) return;
      state.barber = card.getAttribute("data-barber");
      markSelected(wrap, "barber", state.barber);
      state.startMinutes = null;
    });
  }

  function markSelected(wrap, attr, value) {
    wrap.querySelectorAll("[data-" + attr + "]").forEach(function (card) {
      var selected = card.getAttribute("data-" + attr) === value;
      card.classList.toggle("is-selected", selected);
      card.setAttribute("aria-pressed", String(selected));
    });
  }

  /* ---------------- Step 3: Date & time ---------------- */
  function bindDateInput() {
    var input = document.getElementById("bookingDate");
    if (!input) return;
    var today = new Date();
    input.min = toDateInputValue(today);
    var maxDate = new Date(today.getTime() + 60 * 24 * 60 * 60 * 1000);
    input.max = toDateInputValue(maxDate);
    input.addEventListener("change", function () {
      state.date = input.value;
      state.startMinutes = null;
      renderTimeSlots();
    });
  }

  function toDateInputValue(d) {
    return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
  }

  function renderDateTimeStep() {
    var input = document.getElementById("bookingDate");
    if (input && state.date) input.value = state.date;
    renderTimeSlots();
  }

  function renderTimeSlots() {
    var grid = document.getElementById("timeGrid");
    var note = document.getElementById("timeGridNote");
    if (!grid) return;
    grid.innerHTML = "";
    if (!state.date || !state.service) {
      if (note) note.textContent = "Pick a date to see available times.";
      return;
    }
    if (note) note.textContent = "Checking availability…";

    // Slot availability now comes from the backend (source of truth for
    // what's actually booked), not localStorage — this is what prevents two
    // customers from being shown, and taking, the same slot.
    window.STOEP_API.getAvailability(state.date, state.service, state.barber)
      .then(function (res) { renderSlotButtons(grid, note, res.slots); })
      .catch(function (err) {
        if (note) note.textContent = "Couldn't load availability (" + err.message + "). Try again.";
      });
  }

  function renderSlotButtons(grid, note, slots) {
    grid.innerHTML = "";
    if (!slots.length) {
      if (note) note.textContent = "We're closed that day, or fully booked. Try another date.";
      return;
    }
    if (note) note.textContent = "";

    slots.forEach(function (slot) {
      var btn = document.createElement("button");
      btn.type = "button";
      btn.className = "time-slot";
      btn.textContent = minutesToLabel(slot.start);
      btn.disabled = !slot.available;
      if (state.startMinutes === slot.start) btn.classList.add("is-selected");
      btn.addEventListener("click", function () {
        state.startMinutes = slot.start;
        grid.querySelectorAll(".time-slot").forEach(function (s) { s.classList.remove("is-selected"); });
        btn.classList.add("is-selected");
      });
      grid.appendChild(btn);
    });
  }

  function parseDateInput(dateStr) {
    var parts = dateStr.split("-").map(Number);
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  function minutesToLabel(mins) {
    var h = Math.floor(mins / 60), m = mins % 60;
    var period = h >= 12 ? "PM" : "AM";
    var h12 = h % 12 === 0 ? 12 : h % 12;
    return h12 + ":" + String(m).padStart(2, "0") + " " + period;
  }

  function minutesToTimeString(mins) {
    var h = Math.floor(mins / 60), m = mins % 60;
    return String(h).padStart(2, "0") + ":" + String(m).padStart(2, "0");
  }

  /* ---------------- Step 4: Summary + details form ---------------- */
  function renderSummary() {
    var box = document.getElementById("bookingSummary");
    if (!box) return;
    var service = SERVICES.find(function (s) { return s.id === state.service; });
    var barber = BARBERS.find(function (b) { return b.id === state.barber; });
    var dateLabel = state.date ? formatDateLong(parseDateInput(state.date)) : "—";
    var timeLabel = state.startMinutes !== null ? minutesToLabel(state.startMinutes) + " – " + minutesToLabel(state.startMinutes + service.duration) : "—";

    box.innerHTML =
      "<dl>" +
      "<dt>Service</dt><dd>" + service.name + " (R" + service.price + ")</dd>" +
      "<dt>Barber</dt><dd>" + barber.name + "</dd>" +
      "<dt>Date</dt><dd>" + dateLabel + "</dd>" +
      "<dt>Time</dt><dd>" + timeLabel + "</dd>" +
      "</dl>";
  }

  function formatDateLong(date) {
    return date.toLocaleDateString("en-ZA", { weekday: "long", year: "numeric", month: "long", day: "numeric" });
  }

  /* ---------------- Submit / validation ---------------- */
  function handleSubmit(e) {
    e.preventDefault();
    var name = document.getElementById("custName");
    var email = document.getElementById("custEmail");
    var phone = document.getElementById("custPhone");
    var notes = document.getElementById("custNotes");

    var valid = true;
    valid = validateField(name, function (v) { return v.trim().length >= 2; }, "Enter your full name.") && valid;
    valid = validateField(email, function (v) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim()); }, "Enter a valid email address.") && valid;
    valid = validateField(phone, function (v) {
      var cleaned = v.replace(/[\s-]/g, "");
      return /^(\+27\d{9}|0\d{9})$/.test(cleaned);
    }, "Enter a valid South African phone number (e.g. 082 123 4567).") && valid;

    if (!state.service || !state.barber || !state.date || state.startMinutes === null) {
      valid = false;
    }

    if (!valid) return;

    var submitBtn = e.target.querySelector("[type='submit']");
    if (submitBtn) submitBtn.disabled = true;
    var msg = document.getElementById("stepError");
    if (msg) msg.style.display = "none";

    window.STOEP_API.createBooking({
      serviceId: state.service,
      barberId: state.barber,
      date: state.date,
      startMinutes: state.startMinutes,
      customerName: name.value.trim(),
      customerEmail: email.value.trim(),
      customerPhone: phone.value.trim(),
      notes: notes ? notes.value.trim() : ""
    }).then(function (saved) {
      renderConfirmation({
        id: saved.reference,
        serviceName: saved.serviceName,
        price: saved.price,
        barberName: saved.barberName,
        date: saved.date,
        startMinutes: saved.startMinutes,
        endMinutes: saved.endMinutes,
        customerName: saved.customerName,
        customerEmail: saved.customerEmail
      });
    }).catch(function (err) {
      if (submitBtn) submitBtn.disabled = false;
      // 409 = someone else grabbed this exact slot between us showing it and
      // submitting. Send them back to re-pick a time instead of pretending
      // the booking went through.
      if (err.status === 409) {
        goToStep(state.steps.indexOf("datetime"));
        state.startMinutes = null;
        renderTimeSlots();
      }
      if (msg) { msg.textContent = err.message; msg.style.display = "block"; }
    });
  }

  function validateField(input, test, message) {
    if (!input) return true;
    var errorEl = document.getElementById(input.id + "Error");
    var ok = test(input.value || "");
    input.setAttribute("aria-invalid", String(!ok));
    if (errorEl) errorEl.textContent = ok ? "" : message;
    return ok;
  }

  [/* live-clear errors on input */].forEach(function () {});
  document.addEventListener("input", function (e) {
    if (e.target.matches && e.target.matches("#custName, #custEmail, #custPhone")) {
      e.target.setAttribute("aria-invalid", "false");
      var errorEl = document.getElementById(e.target.id + "Error");
      if (errorEl) errorEl.textContent = "";
    }
  });

  /* ---------------- Confirmation ---------------- */
  function renderConfirmation(booking) {
    var wizard = document.getElementById("bookingWizard");
    var confirmation = document.getElementById("bookingConfirmation");
    if (wizard) wizard.style.display = "none";
    if (!confirmation) return;
    confirmation.style.display = "block";

    var dateObj = parseDateInput(booking.date);
    var dateLabel = formatDateLong(dateObj);
    var startLabel = minutesToLabel(booking.startMinutes);
    var endLabel = minutesToLabel(booking.endMinutes);

    confirmation.innerHTML =
      '<div class="confirmation-card">' +
      '<p class="confirmation-ref">Booking reference: <strong>' + booking.id + "</strong></p>" +
      "<h2>You're booked in, " + escapeHtml(firstName(booking.customerName)) + ".</h2>" +
      "<p>A confirmation has been prepared for " + escapeHtml(booking.customerEmail) + ". Add it to your calendar so you don't forget.</p>" +
      '<dl class="confirmation-grid">' +
      "<div><dt>Shop</dt><dd>" + SHOP.name + "</dd></div>" +
      "<div><dt>Service</dt><dd>" + booking.serviceName + " (R" + booking.price + ")</dd></div>" +
      "<div><dt>Barber</dt><dd>" + booking.barberName + "</dd></div>" +
      "<div><dt>Date</dt><dd>" + dateLabel + "</dd></div>" +
      "<div><dt>Time</dt><dd>" + startLabel + " – " + endLabel + "</dd></div>" +
      "<div><dt>Location</dt><dd>" + SHOP.fullAddress + "</dd></div>" +
      "<div><dt>Contact</dt><dd>" + SHOP.phoneDisplay + "</dd></div>" +
      "</dl>" +
      '<div class="confirmation-actions">' +
      '<a class="btn btn-primary" id="googleCalBtn" href="#" target="_blank" rel="noopener">Add to Google Calendar</a>' +
      '<button class="btn btn-outline" id="icsBtn" type="button">Download calendar event (.ics)</button>' +
      '<a class="btn btn-outline" href="index.html">Back to home</a>' +
      "</div>" +
      "</div>";

    var googleBtn = document.getElementById("googleCalBtn");
    if (googleBtn) googleBtn.href = buildGoogleCalendarUrl(booking);
    var icsBtn = document.getElementById("icsBtn");
    if (icsBtn) icsBtn.addEventListener("click", function () { downloadIcs(booking); });

    confirmation.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function firstName(full) { return (full || "").trim().split(/\s+/)[0] || full; }
  function escapeHtml(str) {
    return String(str).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }

  /* ---------------- Calendar generation ---------------- */
  function bookingToDates(booking) {
    var d = parseDateInput(booking.date);
    var start = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0);
    start.setMinutes(booking.startMinutes);
    var end = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0);
    end.setMinutes(booking.endMinutes);
    return { start: start, end: end };
  }

  function pad(n) { return String(n).padStart(2, "0"); }

  function formatFloatingDateTime(d) {
    return d.getFullYear() + pad(d.getMonth() + 1) + pad(d.getDate()) + "T" + pad(d.getHours()) + pad(d.getMinutes()) + "00";
  }

  function buildDescription(booking) {
    return [
      "Service: " + booking.serviceName,
      "Barber: " + booking.barberName,
      "Customer: " + booking.customerName,
      "Booking reference: " + booking.id,
      "Shop contact: " + SHOP.phoneDisplay + " / " + SHOP.email
    ].join("\\n");
  }

  function buildGoogleCalendarUrl(booking) {
    var dates = bookingToDates(booking);
    var params = new URLSearchParams({
      action: "TEMPLATE",
      text: SHOP.name + " — " + booking.serviceName,
      dates: formatFloatingDateTime(dates.start) + "/" + formatFloatingDateTime(dates.end),
      details: buildDescription(booking).replace(/\\n/g, "\n"),
      location: SHOP.fullAddress,
      ctz: "Africa/Johannesburg"
    });
    return "https://www.google.com/calendar/render?" + params.toString();
  }

  function downloadIcs(booking) {
    var dates = bookingToDates(booking);
    var now = new Date();
    var ics = [
      "BEGIN:VCALENDAR",
      "VERSION:2.0",
      "PRODID:-//Stoep Barbershop//Booking//EN",
      "CALSCALE:GREGORIAN",
      "METHOD:PUBLISH",
      "BEGIN:VEVENT",
      "UID:" + booking.id + "@stoepbarbers.co.za",
      "DTSTAMP:" + formatFloatingDateTime(now) + "Z",
      "DTSTART:" + formatFloatingDateTime(dates.start),
      "DTEND:" + formatFloatingDateTime(dates.end),
      "SUMMARY:" + SHOP.name + " — " + booking.serviceName,
      "DESCRIPTION:" + buildDescription(booking),
      "LOCATION:" + SHOP.fullAddress,
      "END:VEVENT",
      "END:VCALENDAR"
    ].join("\r\n");

    var blob = new Blob([ics], { type: "text/calendar;charset=utf-8" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = "stoep-booking-" + booking.id + ".ics";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
})();
