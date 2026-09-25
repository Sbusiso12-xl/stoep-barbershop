/* Stoep Barbershop — tiny fetch wrapper for the backend API.
   Set API_BASE to wherever StoepBarbershop.Api is running. */
window.STOEP_API = (function () {
  "use strict";

  var API_BASE = "https://stoep-barbershop-api.onrender.com";
  var TOKEN_KEY = "stoep_dashboard_token";

  function getToken() { return localStorage.getItem(TOKEN_KEY); }
  function setToken(t) { localStorage.setItem(TOKEN_KEY, t); }
  function clearToken() { localStorage.removeItem(TOKEN_KEY); }

  async function request(path, options) {
    options = options || {};
    var headers = Object.assign({ "Content-Type": "application/json" }, options.headers || {});
    var token = getToken();
    if (token) headers["Authorization"] = "Bearer " + token;

    var res = await fetch(API_BASE + path, {
      method: options.method || "GET",
      headers: headers,
      body: options.body ? JSON.stringify(options.body) : undefined
    });

    var data = null;
    try { data = await res.json(); } catch (e) { /* no body */ }

    if (!res.ok) {
      var message = (data && (data.error || (data.errors && Object.values(data.errors).flat().join(" ")))) || ("Request failed (" + res.status + ")");
      var err = new Error(message);
      err.status = res.status;
      err.body = data;
      throw err;
    }
    return data;
  }

  return {
    getServices: function () { return request("/services"); },
    getBarbers: function () { return request("/barbers"); },
    getAvailability: function (date, serviceId, barberId) {
      var q = "?date=" + encodeURIComponent(date) + "&serviceId=" + encodeURIComponent(serviceId);
      if (barberId) q += "&barberId=" + encodeURIComponent(barberId);
      return request("/availability" + q);
    },
    createBooking: function (payload) { return request("/bookings", { method: "POST", body: payload }); },
    getBookingByReference: function (reference) { return request("/bookings/" + encodeURIComponent(reference)); },

    login: function (username, password) {
      return request("/auth/login", { method: "POST", body: { username: username, password: password } })
        .then(function (res) { setToken(res.token); return res; });
    },
    logout: clearToken,
    isLoggedIn: function () { return !!getToken(); },

    getDashboardBookings: function (params) {
      params = params || {};
      var q = [];
      if (params.barberId) q.push("barberId=" + encodeURIComponent(params.barberId));
      if (params.from) q.push("from=" + encodeURIComponent(params.from));
      if (params.to) q.push("to=" + encodeURIComponent(params.to));
      return request("/dashboard/bookings" + (q.length ? "?" + q.join("&") : ""));
    },
    updateBookingStatus: function (id, status) {
      return request("/dashboard/bookings/" + id + "/status", { method: "PATCH", body: { status: status } });
    }
  };
})();
