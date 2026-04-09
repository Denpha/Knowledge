(function () {
  const STORAGE_KEY = "skckms.swagger.jwt";
  const MODAL_CONTAINER_SELECTOR = ".auth-container";
  const LOGIN_PANEL_ID = "skckms-swagger-login-panel";
  const ACTIONS_ROW_ID = "skckms-swagger-actions-row";
  const LOGIN_BUTTON_ID = "skckms-login-button";
  const LOGOUT_BUTTON_ID = "skckms-logout-button";
  const CLOSE_BUTTON_ID = "skckms-close-button";

  function getSwaggerUi() {
    return window.ui || null;
  }

  function applyToken(token) {
    if (!token) {
      return;
    }

    const ui = getSwaggerUi();
    if (!ui) {
      return;
    }

    try {
      if (ui.authActions && typeof ui.authActions.authorize === "function") {
        ui.authActions.authorize({
          Bearer: {
            name: "Bearer",
            value: token
          }
        });
      }

      // Keep this as a fallback for Swagger UI variants.
      if (typeof ui.preauthorizeApiKey === "function") {
        ui.preauthorizeApiKey("Bearer", token);
      }

      localStorage.setItem(STORAGE_KEY, token);
      console.info("Swagger auto-authorized with JWT token.");
    } catch (error) {
      console.warn("Swagger auto-authorize failed:", error);
    }
  }

  function extractToken(payload) {
    if (!payload || typeof payload !== "object") {
      return null;
    }

    if (typeof payload.token === "string" && payload.token.length > 0) {
      return payload.token;
    }

    if (typeof payload.accessToken === "string" && payload.accessToken.length > 0) {
      return payload.accessToken;
    }

    return null;
  }

  function isLoginRequest(url, method) {
    if (!url || !method) {
      return false;
    }

    const normalizedMethod = String(method).toUpperCase();
    const normalizedUrl = String(url).toLowerCase();
    return normalizedMethod === "POST" && normalizedUrl.includes("/api/auth/login");
  }

  function interceptFetch() {
    const originalFetch = window.fetch;
    if (typeof originalFetch !== "function") {
      return;
    }

    window.fetch = async function (input, init) {
      const response = await originalFetch(input, init);

      try {
        const reqMethod = (init && init.method) || "GET";
        const reqUrl = typeof input === "string" ? input : (input && input.url) || "";

        if (response.ok && isLoginRequest(reqUrl, reqMethod)) {
          const data = await response.clone().json();
          const token = extractToken(data);
          applyToken(token);
        }
      } catch (error) {
        console.warn("Swagger login interceptor error:", error);
      }

      return response;
    };
  }

  function applyStoredToken() {
    const token = localStorage.getItem(STORAGE_KEY);
    if (token) {
      applyToken(token);
    }
  }

  function clearAuthorizationState() {
    const ui = getSwaggerUi();
    if (ui && ui.authActions && typeof ui.authActions.logout === "function") {
      try {
        ui.authActions.logout(["Bearer"]);
      } catch (_error) {
      }
    }

    try {
      localStorage.removeItem(STORAGE_KEY);
      // Swagger UI may persist auth state in these keys depending on version/plugins.
      localStorage.removeItem("authorized");
      localStorage.removeItem("swagger_authorization");
      localStorage.removeItem("swagger-ui.auth");
    } catch (_error) {
    }
  }

  async function loginWithCredentials(username, password, statusEl) {
    try {
      const response = await fetch("/api/Auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ username: username, password: password })
      });

      let payload = null;
      try {
        payload = await response.json();
      } catch (_) {
        payload = null;
      }

      if (!response.ok) {
        statusEl.textContent = "Login failed: username or password is incorrect.";
        statusEl.style.color = "#b91c1c";
        return;
      }

      const token = extractToken(payload);
      if (!token) {
        statusEl.textContent = "Login succeeded but token was not found in response.";
        statusEl.style.color = "#b91c1c";
        return;
      }

      applyToken(token);
      statusEl.textContent = "Login success: token applied to Bearer authorization.";
      statusEl.style.color = "#166534";
    } catch (error) {
      console.warn("Swagger credential login failed:", error);
      statusEl.textContent = "Login request failed. Check API availability.";
      statusEl.style.color = "#b91c1c";
    }
  }

  function createLoginPanel() {
    const panel = document.createElement("div");
    panel.id = LOGIN_PANEL_ID;
    panel.style.borderTop = "1px solid #e5e7eb";
    panel.style.marginTop = "12px";
    panel.style.paddingTop = "12px";

    const title = document.createElement("h4");
    title.textContent = "Login with Username/Password";
    title.style.margin = "0 0 8px";
    title.style.fontSize = "14px";

    const usernameInput = document.createElement("input");
    usernameInput.type = "text";
    usernameInput.placeholder = "Username";
    usernameInput.style.flex = "1 1 220px";
    usernameInput.style.padding = "6px 8px";
    usernameInput.style.boxSizing = "border-box";

    const passwordInput = document.createElement("input");
    passwordInput.type = "password";
    passwordInput.placeholder = "Password";
    passwordInput.style.flex = "1 1 220px";
    passwordInput.style.padding = "6px 8px";
    passwordInput.style.boxSizing = "border-box";

    const button = document.createElement("button");
    button.id = LOGIN_BUTTON_ID;
    button.type = "button";
    button.textContent = "Login and Authorize";
    button.style.padding = "7px 12px";
    button.style.cursor = "pointer";
    button.style.whiteSpace = "nowrap";

    const status = document.createElement("div");
    status.style.marginTop = "8px";
    status.style.fontSize = "12px";

    const actionsRow = document.createElement("div");
    actionsRow.id = ACTIONS_ROW_ID;
    actionsRow.style.display = "none";
    actionsRow.style.gap = "8px";
    actionsRow.style.alignItems = "center";
    actionsRow.style.marginTop = "8px";

    const logoutButton = document.createElement("button");
    logoutButton.id = LOGOUT_BUTTON_ID;
    logoutButton.type = "button";
    logoutButton.textContent = "Logout";
    logoutButton.style.padding = "7px 12px";
    logoutButton.style.cursor = "pointer";

    const closeButton = document.createElement("button");
    closeButton.id = CLOSE_BUTTON_ID;
    closeButton.type = "button";
    closeButton.textContent = "Close";
    closeButton.style.padding = "7px 12px";
    closeButton.style.cursor = "pointer";

    button.addEventListener("click", async function () {
      const username = usernameInput.value.trim();
      const password = passwordInput.value;

      if (!username || !password) {
        status.textContent = "Please enter both username and password.";
        status.style.color = "#b91c1c";
        return;
      }

      button.disabled = true;
      status.textContent = "Logging in...";
      status.style.color = "#374151";

      await loginWithCredentials(username, password, status);
      updateModalButtonsVisibility();
      button.disabled = false;
    });

    logoutButton.addEventListener("click", function () {
      const ui = getSwaggerUi();
      if (ui && ui.authActions && typeof ui.authActions.logout === "function") {
        ui.authActions.logout(["Bearer"]);
      }

      try {
        localStorage.removeItem(STORAGE_KEY);
      } catch (_error) {
      }

      status.textContent = "Logged out. Bearer authorization cleared.";
      status.style.color = "#374151";
      updateModalButtonsVisibility();
    });

    closeButton.addEventListener("click", function () {
      const nativeClose = document.querySelector(".auth-container .modal-ux-header button");
      if (nativeClose && typeof nativeClose.click === "function") {
        nativeClose.click();
      }
    });

    const row = document.createElement("div");
    row.style.display = "flex";
    row.style.flexWrap = "wrap";
    row.style.gap = "8px";
    row.style.alignItems = "center";

    row.appendChild(usernameInput);
    row.appendChild(passwordInput);
    row.appendChild(button);

    actionsRow.appendChild(logoutButton);
    actionsRow.appendChild(closeButton);

    panel.appendChild(title);
    panel.appendChild(row);
    panel.appendChild(actionsRow);
    panel.appendChild(status);
    return panel;
  }

  function isAuthorized() {
    const ui = getSwaggerUi();
    if (!ui || !ui.authSelectors || typeof ui.authSelectors.authorized !== "function") {
      return false;
    }

    try {
      const state = ui.authSelectors.authorized();
      const authObject = state && typeof state.toJS === "function" ? state.toJS() : state;
      return !!(authObject && authObject.Bearer);
    } catch (_error) {
      return false;
    }
  }

  function updateModalButtonsVisibility() {
    const authContainer = document.querySelector(MODAL_CONTAINER_SELECTOR);
    if (!authContainer) {
      return;
    }

    const actions = authContainer.querySelector(".auth-btn-wrapper");
    if (!actions) {
      return;
    }

    // Hide Swagger default action buttons and use our custom actions row.
    actions.style.display = "none";

    const loginButton = document.getElementById(LOGIN_BUTTON_ID);
    const actionsRow = document.getElementById(ACTIONS_ROW_ID);
    const authorized = isAuthorized();

    if (loginButton) {
      loginButton.style.display = authorized ? "none" : "inline-block";
    }

    if (actionsRow) {
      actionsRow.style.display = authorized ? "flex" : "none";
    }
  }

  function syncOperationLockIcons() {
    const authorized = isAuthorized();
    // Target both per-operation buttons AND the main header Authorize button
    const authButtons = document.querySelectorAll("button.authorization__btn, .btn.authorize");

    authButtons.forEach(function (btn) {
      const useEl = btn.querySelector("use");
      if (authorized) {
        btn.classList.remove("locked");
        btn.classList.add("unlocked");
        btn.setAttribute("aria-label", "authorization button unlocked");
        if (useEl) {
          useEl.setAttribute("href", "#unlocked");
          useEl.setAttribute("xlink:href", "#unlocked");
        }
      } else {
        btn.classList.remove("unlocked");
        btn.classList.add("locked");
        btn.setAttribute("aria-label", "authorization button locked");
        if (useEl) {
          useEl.setAttribute("href", "#locked");
          useEl.setAttribute("xlink:href", "#locked");
        }
      }
    });
  }

  function ensureLoginPanel() {
    const authContainer = document.querySelector(MODAL_CONTAINER_SELECTOR);
    if (!authContainer) {
      return;
    }

    if (document.getElementById(LOGIN_PANEL_ID)) {
      updateModalButtonsVisibility();
      return;
    }

    authContainer.appendChild(createLoginPanel());
    updateModalButtonsVisibility();
  }

  function boot() {
    interceptFetch();

    // Wait for Swagger UI to initialize before applying cached token.
    const timer = setInterval(function () {
      if (getSwaggerUi()) {
        clearInterval(timer);
        applyStoredToken();
        updateModalButtonsVisibility();
        syncOperationLockIcons();
      }
    }, 200);

    function initLoginPanelWatcher() {
      if (!document.body) {
        return;
      }

      // Observe modal openings and inject Username/Password panel.
      const observer = new MutationObserver(function () {
        ensureLoginPanel();
      });

      observer.observe(document.body, { childList: true, subtree: true });

      // Fallback polling in case the modal DOM updates without matching observer timing.
      setInterval(function () {
        ensureLoginPanel();
        updateModalButtonsVisibility();
        syncOperationLockIcons();
      }, 300);

      // Initial sync for already-rendered operation rows.
      syncOperationLockIcons();
    }

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", initLoginPanelWatcher, { once: true });
    } else {
      initLoginPanelWatcher();
    }
  }

  boot();
})();