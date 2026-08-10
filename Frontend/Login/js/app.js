const AUTH_API_URL = window.SopmineApi.endpoints.login;

const auth = window.SopmineAuth;
const loginForm = document.getElementById("login-form");
const loginFeedback = document.getElementById("login-feedback");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const passwordToggle = document.getElementById("password-toggle");
const rememberInput = document.getElementById("remember-me");
const forgotPasswordButton = document.getElementById("forgot-password");
const passwordHelpDialog = document.getElementById("password-help-dialog");
const passwordHelpClose = document.getElementById("password-help-close");
const submitButton = document.getElementById("login-submit");
const submitIcon = document.getElementById("login-submit-icon");
const submitLabel = document.getElementById("login-submit-label");
const themeButton = document.getElementById("theme-toggle");
const themeIcon = document.getElementById("theme-toggle-icon");
const themeLabel = document.getElementById("theme-toggle-label");
const LOGIN_MAX_ATTEMPTS = 2;
const LOGIN_RETRY_DELAY_MS = 650;
const LOGIN_REQUEST_TIMEOUT_MS = 12000;
const { show: showFeedback, hide: hideFeedback } =
  window.SopmineFeedback.createFeedback(loginFeedback);

function applyLoginTheme(theme, persist = true) {
  const light = theme === "light";
  document.documentElement.classList.toggle("theme-light", light);
  document.documentElement.classList.toggle("dark", !light);
  themeIcon.textContent = light ? "dark_mode" : "light_mode";
  themeLabel.textContent = light ? "Mode sombre" : "Mode clair";
  themeButton.setAttribute("aria-label", light ? "Activer le mode sombre" : "Activer le mode clair");
  if (persist) localStorage.setItem("sopmine-design-theme", light ? "light" : "dark");
}

function toggleLoginTheme() {
  const nextTheme = document.documentElement.classList.contains("dark") ? "light" : "dark";
  if (typeof document.startViewTransition === "function") {
    document.startViewTransition(() => applyLoginTheme(nextTheme));
    return;
  }
  applyLoginTheme(nextTheme);
}

function setSubmittingState(isSubmitting) {
  submitButton.disabled = isSubmitting;
  submitIcon.textContent = isSubmitting ? "progress_activity" : "login";
  submitLabel.textContent = isSubmitting ? "Connexion…" : "Entrer dans Sopmine";
}

function openPasswordHelp() {
  passwordHelpDialog?.showModal();
}

function closePasswordHelp() {
  passwordHelpDialog?.close();
}

function togglePasswordVisibility() {
  if (!passwordInput || !passwordToggle) {
    return;
  }

  const isVisible = passwordInput.type === "text";
  passwordInput.type = isVisible ? "password" : "text";

  const label = isVisible
    ? "Afficher le mot de passe"
    : "Masquer le mot de passe";

  passwordToggle.setAttribute("aria-label", label);
  passwordToggle.setAttribute("title", label);
  passwordToggle.querySelector(".material-symbols-outlined").textContent =
    isVisible ? "visibility" : "visibility_off";
}

function extractErrorMessage(data, response = null) {
  if (response?.status === 429) {
    return "Trop de tentatives de connexion. Patientez un instant puis reessayez.";
  }

  if (typeof data?.title === "string" && data.title.trim()) {
    return data.title.trim();
  }

  if (typeof data?.description === "string" && data.description.trim()) {
    return data.description.trim();
  }

  if (data?.errors && typeof data.errors === "object") {
    const firstEntry = Object.values(data.errors).find(
      (value) => Array.isArray(value) && value.length > 0,
    );

    if (Array.isArray(firstEntry) && firstEntry[0]) {
      return String(firstEntry[0]).trim();
    }
  }

  return "Impossible de se connecter pour le moment.";
}

function wait(delayMs) {
  return new Promise((resolve) => {
    window.setTimeout(resolve, delayMs);
  });
}

function shouldRetryLoginResponse(response) {
  return response.status === 408 || response.status === 429 || response.status >= 500;
}

async function fetchLoginResponse(email, password) {
  let lastError = null;

  for (let attempt = 1; attempt <= LOGIN_MAX_ATTEMPTS; attempt += 1) {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(
      () => controller.abort(),
      LOGIN_REQUEST_TIMEOUT_MS,
    );

    try {
      const response = await fetch(AUTH_API_URL, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        signal: controller.signal,
        body: JSON.stringify({ email, password }),
      });

      if (attempt < LOGIN_MAX_ATTEMPTS && shouldRetryLoginResponse(response)) {
        await wait(LOGIN_RETRY_DELAY_MS);
        continue;
      }

      return response;
    } catch (error) {
      lastError = error;

      if (attempt < LOGIN_MAX_ATTEMPTS) {
        await wait(LOGIN_RETRY_DELAY_MS);
        continue;
      }
    } finally {
      window.clearTimeout(timeoutId);
    }
  }

  throw lastError ?? new Error("Login request failed.");
}

function getFallbackRedirectUrl() {
  const defaultPath = auth?.DEFAULT_APP_PATH || "/Dashboard/";
  const currentUrl = new URL(window.location.href);
  const returnUrl = currentUrl.searchParams.get("returnUrl");

  if (returnUrl) {
    try {
      const resolvedUrl = new URL(returnUrl, window.location.origin);
      const resolvedPath = resolvedUrl.pathname.replace(/\/+$/, "").toLowerCase();

      if (
        resolvedUrl.origin === window.location.origin &&
        resolvedPath !== "/login" &&
        resolvedPath !== "/login/index.html"
      ) {
        return resolvedUrl;
      }
    } catch (error) {
      // Fall back to the default app page.
    }
  }

  return new URL(defaultPath, window.location.origin);
}

function redirectAfterSuccessfulLogin() {
  if (typeof auth?.redirectAfterLogin === "function") {
    auth.redirectAfterLogin();

    window.setTimeout(() => {
      if (auth?.hasValidSession?.()) {
        window.location.assign(getFallbackRedirectUrl().toString());
      }
    }, 900);

    return;
  }

  window.location.replace(getFallbackRedirectUrl().toString());
}

async function submitLogin(event) {
  event.preventDefault();
  hideFeedback();

  if (!loginForm.reportValidity()) {
    return;
  }

  setSubmittingState(true);

  try {
    const email = emailInput.value.trim();
    const response = await fetchLoginResponse(email, passwordInput.value);

    const data = await response.json().catch(() => null);

    if (!response.ok) {
      showFeedback("error", extractErrorMessage(data, response));
      return;
    }

    if (
      typeof data?.accessToken !== "string" ||
      typeof data?.expiresOnUtc !== "string"
    ) {
      showFeedback("error", "La reponse de connexion est incomplete.");
      return;
    }

    auth?.setSession({
      accessToken: data.accessToken,
      expiresOnUtc: data.expiresOnUtc,
      email,
    }, { persistent: rememberInput?.checked !== false });

    showFeedback("success", "Connexion reussie. Redirection...");
    redirectAfterSuccessfulLogin();
  } catch (error) {
    showFeedback(
      "error",
      "Connexion impossible pour le moment. L'API est peut-etre encore en demarrage, reessayez dans quelques secondes.",
    );
  } finally {
    setSubmittingState(false);
  }
}

if (auth?.hasValidSession()) {
  auth.redirectAfterLogin();
}

applyLoginTheme(document.documentElement.classList.contains("dark") ? "dark" : "light", false);

loginForm?.addEventListener("submit", submitLogin);
passwordToggle?.addEventListener("click", togglePasswordVisibility);
forgotPasswordButton?.addEventListener("click", openPasswordHelp);
passwordHelpClose?.addEventListener("click", closePasswordHelp);
themeButton?.addEventListener("click", toggleLoginTheme);
