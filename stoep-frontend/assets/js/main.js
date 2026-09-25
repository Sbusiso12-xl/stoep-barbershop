/* Stoep Barbershop — shared site behaviour: nav, footer content, first-visit modal */
(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    initNav();
    fillShopDetails();
    initFirstVisitModal();
    initYear();
  });

  function initNav() {
    var toggle = document.querySelector(".nav-toggle");
    var nav = document.querySelector(".main-nav");
    if (!toggle || !nav) return;

    toggle.addEventListener("click", function () {
      var isOpen = nav.classList.toggle("is-open");
      toggle.setAttribute("aria-expanded", String(isOpen));
      document.body.style.overflow = isOpen ? "hidden" : "";
    });

    nav.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        nav.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
        document.body.style.overflow = "";
      });
    });

    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && nav.classList.contains("is-open")) {
        nav.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
        document.body.style.overflow = "";
        toggle.focus();
      }
    });
  }

  function fillShopDetails() {
    document.querySelectorAll("[data-shop-phone]").forEach(function (el) { el.textContent = SHOP.phoneDisplay; });
    document.querySelectorAll("[data-shop-phone-href]").forEach(function (el) { el.setAttribute("href", "tel:" + SHOP.phoneHref); });
    document.querySelectorAll("[data-shop-email]").forEach(function (el) { el.textContent = SHOP.email; });
    document.querySelectorAll("[data-shop-email-href]").forEach(function (el) { el.setAttribute("href", "mailto:" + SHOP.email); });
    document.querySelectorAll("[data-shop-address]").forEach(function (el) { el.textContent = SHOP.fullAddress; });
    document.querySelectorAll("[data-shop-address-1]").forEach(function (el) { el.textContent = SHOP.addressLine1; });
    document.querySelectorAll("[data-shop-address-2]").forEach(function (el) { el.textContent = SHOP.addressLine2; });
    document.querySelectorAll("[data-shop-instagram]").forEach(function (el) { el.setAttribute("href", SHOP.instagram); });
    document.querySelectorAll("[data-shop-facebook]").forEach(function (el) { el.setAttribute("href", SHOP.facebook); });

    var hoursLists = document.querySelectorAll("[data-shop-hours]");
    hoursLists.forEach(function (list) {
      list.innerHTML = "";
      SHOP.hoursDisplay.forEach(function (row) {
        var li = document.createElement("li");
        li.innerHTML = "<span>" + row.label + "</span><span>" + row.value + "</span>";
        list.appendChild(li);
      });
    });
  }

  function initYear() {
    document.querySelectorAll("[data-year]").forEach(function (el) {
      el.textContent = new Date().getFullYear();
    });
  }

  function initFirstVisitModal() {
    var overlay = document.getElementById("firstVisitModal");
    if (!overlay) return;
    var STORAGE_KEY = "stoep_first_visit_seen";
    var closeBtn = overlay.querySelector(".modal-close");
    var ctaBtn = overlay.querySelector("[data-modal-cta]");
    var lastFocused = null;

    function openModal() {
      lastFocused = document.activeElement;
      overlay.classList.add("is-open");
      overlay.setAttribute("aria-hidden", "false");
      var focusable = overlay.querySelector("button, a[href]");
      if (focusable) focusable.focus();
      document.addEventListener("keydown", onKeydown);
    }
    function closeModal() {
      overlay.classList.remove("is-open");
      overlay.setAttribute("aria-hidden", "true");
      document.removeEventListener("keydown", onKeydown);
      try { sessionStorage.setItem(STORAGE_KEY, "1"); } catch (e) {}
      if (lastFocused) lastFocused.focus();
    }
    function onKeydown(e) {
      if (e.key === "Escape") closeModal();
      if (e.key === "Tab") trapFocus(e);
    }
    function trapFocus(e) {
      var focusables = overlay.querySelectorAll("button, a[href]");
      if (!focusables.length) return;
      var first = focusables[0], last = focusables[focusables.length - 1];
      if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
      else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    overlay.addEventListener("click", function (e) {
      if (e.target === overlay) closeModal();
    });
    if (closeBtn) closeBtn.addEventListener("click", closeModal);
    if (ctaBtn) ctaBtn.addEventListener("click", function () {
      closeModal();
      window.location.href = "booking.html";
    });

    var alreadySeen = false;
    try { alreadySeen = sessionStorage.getItem(STORAGE_KEY) === "1"; } catch (e) {}
    if (!alreadySeen) {
      window.setTimeout(openModal, 2200);
    }
  }
})();
