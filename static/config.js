window.APP_CONFIG = {
  apiBaseUrl: "",
  defaultHeaders: {}
};

window.AuthUI = {
  currentLocale: "uk",
  catalogs: {},
  defaultLocale: "uk",
  supportedLocales: ["uk", "en"],
  readyPromise: null,

  normalizeLocale(value) {
    const locale = String(value || "").trim().toLowerCase();
    return this.supportedLocales.includes(locale) ? locale : this.defaultLocale;
  },

  getStoredLanguage() {
    const fromStorage = window.localStorage.getItem("ui_lang");
    if (fromStorage) {
      return this.normalizeLocale(fromStorage);
    }
    const cookieMatch = document.cookie.match(/(?:^|;\s*)ui_lang=([^;]+)/);
    if (cookieMatch && cookieMatch[1]) {
      return this.normalizeLocale(decodeURIComponent(cookieMatch[1]));
    }
    return this.defaultLocale;
  },

  persistLanguage(locale) {
    const resolved = this.normalizeLocale(locale);
    window.localStorage.setItem("ui_lang", resolved);
    document.cookie = `ui_lang=${encodeURIComponent(resolved)}; path=/; SameSite=Lax`;
  },

  async loadCatalog(locale) {
    const resolved = this.normalizeLocale(locale);
    if (this.catalogs[resolved]) {
      return this.catalogs[resolved];
    }
    const response = await fetch(`/ui/i18n/${resolved}.json`, { credentials: "same-origin" });
    const payload = await response.json().catch(() => ({}));
    this.catalogs[resolved] = payload || {};
    return this.catalogs[resolved];
  },

  async ensureLocale(locale = "") {
    const resolved = this.normalizeLocale(locale || this.getStoredLanguage());
    await this.loadCatalog("en");
    await this.loadCatalog(this.defaultLocale);
    await this.loadCatalog(resolved);
    this.currentLocale = resolved;
    this.persistLanguage(resolved);
    document.documentElement.lang = resolved;
    this.applyTranslations(document);
    this.attachLanguageSelector(document);
    return resolved;
  },

  async i18nReady() {
    if (!this.readyPromise) {
      this.readyPromise = this.ensureLocale();
    }
    return this.readyPromise;
  },

  t(key, variables = {}, locale = "") {
    const resolved = this.normalizeLocale(locale || this.currentLocale);
    const catalog = this.catalogs[resolved] || {};
    const english = this.catalogs.en || {};
    const fallback = this.catalogs[this.defaultLocale] || {};
    let template = catalog[key] || english[key] || fallback[key] || key;
    Object.entries(variables || {}).forEach(([name, value]) => {
      template = template.replaceAll(`{${name}}`, String(value ?? ""));
    });
    return template;
  },

  applyTranslations(root = document) {
    root.querySelectorAll("[data-i18n]").forEach((node) => {
      node.textContent = this.t(node.getAttribute("data-i18n"));
    });
    root.querySelectorAll("[data-i18n-html]").forEach((node) => {
      node.innerHTML = this.t(node.getAttribute("data-i18n-html"));
    });
    root.querySelectorAll("[data-i18n-placeholder]").forEach((node) => {
      node.setAttribute("placeholder", this.t(node.getAttribute("data-i18n-placeholder")));
    });
    root.querySelectorAll("[data-i18n-title]").forEach((node) => {
      node.setAttribute("title", this.t(node.getAttribute("data-i18n-title")));
    });
  },

  attachLanguageSelector(root = document) {
    const selector = root.querySelector("#languageSelector");
    if (!selector || selector.dataset.bound === "true") {
      if (selector) selector.value = this.currentLocale;
      return;
    }
    selector.value = this.currentLocale;
    selector.addEventListener("change", async (event) => {
      await this.ensureLocale(event.target.value);
      window.location.reload();
    });
    selector.dataset.bound = "true";
  },

  buildUrl(path, query = {}) {
    const base = (window.APP_CONFIG?.apiBaseUrl || "").replace(/\/$/, "");
    const url = new URL((base ? base : window.location.origin) + path, window.location.origin);
    Object.entries(query || {}).forEach(([key, value]) => {
      if (value !== undefined && value !== null && String(value).trim() !== "") {
        url.searchParams.set(key, String(value));
      }
    });
    return url.toString();
  },

  async fetchJson(path, options = {}) {
    await this.i18nReady();
    const response = await fetch(this.buildUrl(path, options.query || {}), {
      method: options.method || "GET",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        "X-Lang": this.currentLocale,
        ...(window.APP_CONFIG?.defaultHeaders || {}),
        ...(options.headers || {})
      },
      body: options.body ? JSON.stringify(options.body) : undefined
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      const error = new Error(payload?.detail?.message || payload?.detail?.error || this.t("common.empty"));
      error.status = response.status;
      error.payload = payload;
      throw error;
    }
    return payload;
  },

  escapeHtml(value) {
    return String(value ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;")
      .replace(/'/g, "&#39;");
  },

  boolLabel(value) {
    return value ? this.t("common.yes") : this.t("common.no");
  },

  formatDateTime(value) {
    if (!value) return "-";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return String(value);
    const pad = (item) => String(item).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
  },

  translateStatus(value) {
    const normalized = String(value || "").trim().toLowerCase();
    return this.t(`status.${normalized}`) !== `status.${normalized}` ? this.t(`status.${normalized}`) : (value || "-");
  },

  translateSeverity(value) {
    const normalized = String(value || "").trim().toLowerCase();
    if (normalized === "critical") return this.currentLocale === "uk" ? "критично" : "critical";
    if (normalized === "warning") return this.currentLocale === "uk" ? "попередження" : "warning";
    if (normalized === "minor") return this.currentLocale === "uk" ? "незначно" : "minor";
    if (normalized === "risk") return this.currentLocale === "uk" ? "ризик" : "risk";
    if (normalized === "info") return this.currentLocale === "uk" ? "інфо" : "info";
    return value || "-";
  },

  translateImpact(value) {
    const normalized = String(value || "").trim().toLowerCase();
    const mapping = {
      "breaks api": this.currentLocale === "uk" ? "ламає API" : "breaks API",
      "changes behavior": this.currentLocale === "uk" ? "змінює поведінку" : "changes behavior",
      "missing validation": this.currentLocale === "uk" ? "немає validation" : "missing validation",
      "risk of regression": this.currentLocale === "uk" ? "ризик регресії" : "risk of regression"
    };
    return mapping[normalized] || value || "-";
  },

  hasAdminAccess(me) {
    return Boolean((me?.capabilities || []).some((cap) => ["user.manage", "role.manage", "policy.manage", "auth.manage"].includes(cap)));
  },

  normalizeMe(payload) {
    if (!payload || typeof payload !== "object" || typeof payload.authenticated !== "boolean") {
      throw new Error(this.t("error.auth_state_invalid"));
    }
    const userId = String(payload.user_id ?? payload.user?.user_id ?? "").trim();
    const username = String(payload.username ?? payload.user?.username ?? "").trim();
    const displayName = String(payload.display_name ?? payload.user?.display_name ?? "").trim();
    const role = String(payload.role ?? payload.user?.role_name ?? "").trim();
    const capabilities = Array.isArray(payload.capabilities)
      ? payload.capabilities.map((item) => String(item || "").trim()).filter(Boolean)
      : [];
    return {
      authenticated: Boolean(payload.authenticated),
      dev_fallback: Boolean(payload.dev_fallback),
      language: this.normalizeLocale(payload.language || this.currentLocale),
      user_id: userId,
      username,
      display_name: displayName,
      role,
      capabilities,
      user: {
        user_id: userId,
        username,
        display_name: displayName,
        role_name: role,
        is_active: Boolean(payload.user?.is_active),
        must_change_password: Boolean(payload.user?.must_change_password)
      }
    };
  },

  applySessionNavigation({ me = null, loginLinkId = "loginLink", logoutLinkId = "logoutLink", logoutButtonId = "logoutButton", adminLinkId = "adminLink" } = {}) {
    const loginLink = document.getElementById(loginLinkId);
    const logoutLink = document.getElementById(logoutLinkId);
    const logoutButton = document.getElementById(logoutButtonId);
    const adminLink = document.getElementById(adminLinkId);
    const authenticated = Boolean(me?.authenticated);
    const canAdmin = authenticated && this.hasAdminAccess(me);

    if (loginLink) {
      loginLink.hidden = authenticated;
    }
    if (logoutLink) {
      logoutLink.hidden = !authenticated;
    }
    if (logoutButton) {
      logoutButton.hidden = !authenticated;
    }
    if (adminLink) {
      adminLink.hidden = !canAdmin;
    }
  },

  renderStateMessage(target, { title = "", message = "", stateClass = "blocked" } = {}) {
    if (!target) {
      return;
    }
    target.hidden = false;
    target.innerHTML = `
      <div class="review-banner ${this.escapeHtml(stateClass)}">
        <h2>${this.escapeHtml(title || this.t("common.message"))}</h2>
        <p>${this.escapeHtml(message || this.t("common.empty"))}</p>
      </div>
    `;
  },

  renderAccessDenied(target, message = "") {
    this.renderStateMessage(target, {
      title: this.t("common.access_denied"),
      message: message || this.t("common.access_denied"),
      stateClass: "blocked"
    });
  },

  async loadMe() {
    const payload = await this.fetchJson("/auth/me");
    return this.normalizeMe(payload);
  },

  async requireLogin({ redirectTo = "/ui/login.html", requireRole = "", allowErrorState = false } = {}) {
    await this.i18nReady();
    try {
      const me = await this.loadMe();
      if (!me.authenticated) {
        window.location.href = redirectTo;
        return null;
      }
      if (requireRole === "admin" && !this.hasAdminAccess(me)) {
        return { ...me, accessDenied: true };
      }
      return me;
    } catch (error) {
      if (allowErrorState) {
        return { authenticated: false, authError: true, errorMessage: error.message || this.t("error.auth_state_invalid"), capabilities: [] };
      }
      if (error?.status === 401) {
        window.location.href = redirectTo;
        return null;
      }
      window.location.href = redirectTo;
      return null;
    }
  },

  async logout() {
    try {
      await this.fetchJson("/auth/logout", { method: "POST" });
    } finally {
      window.location.href = "/ui/login.html";
    }
  }
};

window.AuthUI.i18nReady();
