(() => {
  // Shared authentication layer. It stores the login token, redirects expired
  // sessions to /Login/, and attaches the Bearer token to API requests.
  const AUTH_STORAGE_KEY = "sopmine-auth-session";
  const LOGIN_PATH = "/Login/";
  const DEFAULT_APP_PATH = "/Dashboard/";
  const API_ORIGIN = window.SopmineApi?.origin ?? "http://localhost:5269";
  const API_BASE_URL = window.SopmineApi?.baseUrl ?? `${API_ORIGIN}/api/v1`;
  const API_LOGIN_URL = window.SopmineApi?.endpoints?.login ?? `${API_BASE_URL}/auth/login`;
  const EXPIRY_SAFETY_WINDOW_MS = 30 * 1000;
  const EMPLOYEE_ROLE = "employee";
  const EMPLOYEE_UI_CLASS = "role-employee";
  const USER_PROFILE_OVERRIDES = {
    "soufianeboudchich72@gmail.com": {
      displayName: "Soufiane Boudchich",
    },
  };

  function getCurrentPath() {
    return window.location.pathname.replace(/\/+$/, "").toLowerCase() || "/";
  }

  function isRootPage() {
    const currentPath = getCurrentPath();
    return currentPath === "/" || currentPath === "/index.html";
  }

  function isLoginPage() {
    const currentPath = getCurrentPath();
    return currentPath === "/login" || currentPath === "/login/index.html";
  }

  function readStoredSession() {
    for (const storage of [window.sessionStorage, window.localStorage]) {
      try {
        const rawValue = storage?.getItem?.(AUTH_STORAGE_KEY);
        if (!rawValue) continue;

        const parsed = JSON.parse(rawValue);
        if (
          typeof parsed?.accessToken !== "string" ||
          typeof parsed?.expiresOnUtc !== "string"
        ) {
          continue;
        }

        return {
          accessToken: parsed.accessToken.trim(),
          expiresOnUtc: parsed.expiresOnUtc.trim(),
          email:
            typeof parsed?.email === "string" && parsed.email.trim()
              ? parsed.email.trim()
              : null,
        };
      } catch (error) {
        // Continue with the other storage option.
      }
    }

    return null;
  }

  function getSessionExpiry(session) {
    const timestamp = Date.parse(session?.expiresOnUtc ?? "");
    return Number.isFinite(timestamp) ? timestamp : Number.NaN;
  }

  function isSessionExpired(session) {
    const expiry = getSessionExpiry(session);

    if (!Number.isFinite(expiry)) {
      return true;
    }

    return expiry <= Date.now() + EXPIRY_SAFETY_WINDOW_MS;
  }

  function hasValidSession() {
    const session = readStoredSession();
    return Boolean(session?.accessToken) && !isSessionExpired(session);
  }

  function setSession(session, options = {}) {
    if (
      typeof session?.accessToken !== "string" ||
      typeof session?.expiresOnUtc !== "string"
    ) {
      return;
    }

    const persistent = options?.persistent !== false;
    const targetStorage = persistent ? window.localStorage : window.sessionStorage;
    const serializedSession = JSON.stringify({
      accessToken: session.accessToken.trim(),
      expiresOnUtc: session.expiresOnUtc.trim(),
      email:
        typeof session?.email === "string" && session.email.trim()
          ? session.email.trim()
          : null,
    });

    try {
      window.SopmineApi?.clearCachedGets?.();
      window.localStorage?.removeItem?.(AUTH_STORAGE_KEY);
      window.sessionStorage?.removeItem?.(AUTH_STORAGE_KEY);
      targetStorage?.setItem?.(AUTH_STORAGE_KEY, serializedSession);
    } catch (error) {
      // Ignore storage issues and let the login page handle failures.
    }
  }

  function decodeJwtPayload(token) {
    if (typeof token !== "string" || token.trim() === "") {
      return null;
    }

    try {
      const payloadSegment = token.split(".")[1];

      if (!payloadSegment) {
        return null;
      }

      const normalizedPayload = payloadSegment
        .replaceAll("-", "+")
        .replaceAll("_", "/")
        .padEnd(Math.ceil(payloadSegment.length / 4) * 4, "=");

      const decodedPayload = window.atob(normalizedPayload);
      return JSON.parse(decodedPayload);
    } catch (error) {
      return null;
    }
  }

  function getSessionEmail(session) {
    if (typeof session?.email === "string" && session.email.trim()) {
      return session.email.trim();
    }

    const payload = decodeJwtPayload(session?.accessToken);

    if (typeof payload?.email === "string" && payload.email.trim()) {
      return payload.email.trim();
    }

    return "";
  }

  function getProfileOverride(session) {
    const email = getSessionEmail(session).toLowerCase();
    return USER_PROFILE_OVERRIDES[email] ?? null;
  }

  function getSessionRole(session) {
    return getSessionRoles(session)[0] ?? "";
  }

  function getSessionRoles(session) {
    const payload = decodeJwtPayload(session?.accessToken);
    const roleClaims = [
      payload?.role,
      payload?.roles,
      payload?.Role,
      payload?.Roles,
      payload?.[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ],
      payload?.[
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role"
      ],
    ];

    return roleClaims
      .flatMap((claim) => {
        if (Array.isArray(claim)) {
          return claim;
        }

        if (typeof claim === "string") {
          return claim.includes(",") ? claim.split(",") : [claim];
        }

        return [];
      })
      .map((role) => String(role ?? "").trim())
      .filter(Boolean);
  }

  function isEmployeeSession(session) {
    return getSessionRoles(session).some(
      (role) => role.toLowerCase() === EMPLOYEE_ROLE,
    );
  }

  function syncEmployeeUiState(isEmployee) {
    document.documentElement.classList.toggle(EMPLOYEE_UI_CLASS, isEmployee);
    document.body.classList.toggle(EMPLOYEE_UI_CLASS, isEmployee);
    document.getElementById("app-shell")?.classList.toggle(EMPLOYEE_UI_CLASS, isEmployee);
  }

  function applyRoleVisibility() {
    const session = readStoredSession();
    const isEmployee = Boolean(session && !isSessionExpired(session) && isEmployeeSession(session));

    syncEmployeeUiState(isEmployee);

  }

  function getUserDisplayName(session) {
    const profileOverride = getProfileOverride(session);

    if (typeof profileOverride?.displayName === "string") {
      return profileOverride.displayName.trim();
    }

    const email = getSessionEmail(session);

    if (!email) {
      return "Utilisateur";
    }

    return email.split("@")[0].trim() || email.trim();
  }

  function getUserSecondaryLabel(session) {
    const role = getSessionRole(session);
    return role || "Compte atelier";
  }

  function getUserInitials(displayName) {
    const normalizedValue = String(displayName ?? "")
      .replaceAll(/[^a-z0-9]+/gi, " ")
      .trim();

    if (!normalizedValue) {
      return "AD";
    }

    const parts = normalizedValue.split(/\s+/).filter(Boolean);

    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    }

    return parts[0].slice(0, 2).toUpperCase();
  }

  function applyUserProfile() {
    const session = readStoredSession();

    if (!session || isSessionExpired(session)) {
      return;
    }

    const displayName = getUserDisplayName(session);
    const secondaryLabel = getUserSecondaryLabel(session);
    const initials = getUserInitials(displayName);

    document.querySelectorAll(".profile-chip").forEach((chip) => {
      const nameElement = chip.querySelector(".profile-meta strong");
      const roleElement = chip.querySelector(".profile-meta span");
      const avatarElement = chip.querySelector(".profile-avatar");

      if (nameElement) {
        nameElement.textContent = displayName;
      }

      if (roleElement) {
        roleElement.textContent = secondaryLabel;
      }

      if (avatarElement) {
        avatarElement.textContent = initials;
      }
    });
  }

  function applyAuthenticatedUi() {
    applyUserProfile();
    applyRoleVisibility();
  }

  function clearSession() {
    try {
      window.localStorage?.removeItem?.(AUTH_STORAGE_KEY);
      window.sessionStorage?.removeItem?.(AUTH_STORAGE_KEY);
      window.SopmineApi?.clearCachedGets?.();
    } catch (error) {
      // Ignore storage issues.
    }
  }

  function getSafeReturnUrl(value) {
    if (!value) {
      return null;
    }

    try {
      const resolvedUrl = new URL(value, window.location.origin);

      if (resolvedUrl.origin !== window.location.origin) {
        return null;
      }

      const resolvedPath = resolvedUrl.pathname.replace(/\/+$/, "").toLowerCase();

      if (resolvedPath === "/login" || resolvedPath === "/login/index.html") {
        return DEFAULT_APP_PATH;
      }

      return `${resolvedUrl.pathname}${resolvedUrl.search}${resolvedUrl.hash}`;
    } catch (error) {
      return null;
    }
  }

  function getCurrentReturnUrl() {
    return `${window.location.pathname}${window.location.search}${window.location.hash}`;
  }

  function buildLoginUrl(returnUrl = getCurrentReturnUrl()) {
    const loginUrl = new URL(LOGIN_PATH, window.location.origin);
    const safeReturnUrl = getSafeReturnUrl(returnUrl);

    if (safeReturnUrl) {
      loginUrl.searchParams.set("returnUrl", safeReturnUrl);
    }

    return loginUrl;
  }

  function redirectToLogin(returnUrl) {
    const loginUrl = buildLoginUrl(returnUrl);
    window.location.replace(loginUrl.toString());
  }

  function redirectAfterLogin() {
    const currentUrl = new URL(window.location.href);
    const returnUrl = getSafeReturnUrl(currentUrl.searchParams.get("returnUrl"));
    const nextPath = returnUrl || DEFAULT_APP_PATH;
    const nextUrl = new URL(nextPath, window.location.origin);
    window.location.replace(nextUrl.toString());
  }

  function ensureAuthenticated() {
    const session = readStoredSession();

    if (!session || isSessionExpired(session)) {
      clearSession();
      redirectToLogin();
      return false;
    }

    return true;
  }

  function normalizeRequestPath(value) {
    const normalized = value.toLowerCase().replace(/\/+$/, "");
    return normalized || "/";
  }

  function shouldHandleApiRequest(url) {
    try {
      const requestUrl = new URL(url, window.location.origin);
      const apiBaseUrl = new URL(API_BASE_URL, window.location.origin);
      const requestPath = normalizeRequestPath(requestUrl.pathname);
      const apiBasePath = normalizeRequestPath(apiBaseUrl.pathname);
      return (
        requestUrl.origin === apiBaseUrl.origin &&
        (requestPath === apiBasePath || requestPath.startsWith(`${apiBasePath}/`))
      );
    } catch (error) {
      return false;
    }
  }

  function isLoginRequest(url) {
    try {
      const requestUrl = new URL(url, window.location.origin);
      const loginUrl = new URL(API_LOGIN_URL, window.location.origin);
      return (
        requestUrl.origin === loginUrl.origin &&
        normalizeRequestPath(requestUrl.pathname) === normalizeRequestPath(loginUrl.pathname)
      );
    } catch (error) {
      return false;
    }
  }

  function getRequestUrl(input) {
    if (input instanceof Request) {
      return input.url;
    }

    return String(input ?? "");
  }

  function getRequestMethod(input, init) {
    if (init?.method) {
      return init.method.toUpperCase();
    }

    if (input instanceof Request) {
      return input.method.toUpperCase();
    }

    return "GET";
  }

  function createUnauthorizedResponse() {
    return new Response(
      JSON.stringify({
        description: "Votre session a expire. Merci de vous reconnecter.",
      }),
      {
        status: 401,
        headers: {
          "Content-Type": "application/json",
        },
      },
    );
  }

  function handleUnauthorizedResponse() {
    clearSession();

    if (!isLoginPage() && !isRootPage()) {
      redirectToLogin();
    }
  }

  const nativeFetch = window.fetch.bind(window);

  // Wrap fetch once so normal page code can call fetch/requestJson without
  // manually adding the Authorization header every time.
  window.fetch = async (input, init = {}) => {
    const requestUrl = getRequestUrl(input);
    const requestMethod = getRequestMethod(input, init);

    if (!shouldHandleApiRequest(requestUrl) || isLoginRequest(requestUrl)) {
      return nativeFetch(input, init);
    }

    const session = readStoredSession();

    if (!session || isSessionExpired(session)) {
      handleUnauthorizedResponse();
      return createUnauthorizedResponse();
    }

    if (requestMethod === "GET" && window.SopmineApi?.isCacheableGet?.(requestUrl, init)) {
      const cachedData = window.SopmineApi.readCachedJson(requestUrl);

      if (cachedData !== null) {
        return window.SopmineApi.createCachedResponse(cachedData);
      }
    }

    if (input instanceof Request) {
      const headers = new Headers(input.headers);
      const extraHeaders = new Headers(init?.headers ?? undefined);

      extraHeaders.forEach((value, key) => {
        headers.set(key, value);
      });

      if (!headers.has("Authorization")) {
        headers.set("Authorization", `Bearer ${session.accessToken}`);
      }

      const response = await nativeFetch(
        new Request(input, {
          ...init,
          headers,
        }),
      );

      if (response.status === 401) {
        handleUnauthorizedResponse();
      }

      if (requestMethod === "GET" && response.ok) {
        response.clone().json()
          .then((data) => window.SopmineApi?.writeCachedJson?.(requestUrl, data))
          .catch(() => {});
      } else if (requestMethod !== "GET" && response.ok) {
        window.SopmineApi?.clearCachedGets?.();
      }

      return response;
    }

    const headers = new Headers(init?.headers ?? {});

    if (!headers.has("Authorization")) {
      headers.set("Authorization", `Bearer ${session.accessToken}`);
    }

    const response = await nativeFetch(input, {
      ...init,
      headers,
    });

    if (response.status === 401) {
      handleUnauthorizedResponse();
    }

    if (requestMethod === "GET" && response.ok) {
      response.clone().json()
        .then((data) => window.SopmineApi?.writeCachedJson?.(requestUrl, data))
        .catch(() => {});
    } else if (requestMethod !== "GET" && response.ok) {
      window.SopmineApi?.clearCachedGets?.();
    }

    return response;
  };

  document.addEventListener("click", (event) => {
    const logoutAction = event.target.closest(".logout-action");

    if (!logoutAction) {
      return;
    }

    event.preventDefault();
    clearSession();
    redirectToLogin();
  });

  const initialSession = readStoredSession();

  if (initialSession && isSessionExpired(initialSession)) {
    clearSession();
  }

  window.SopmineAuth = {
    AUTH_STORAGE_KEY,
    LOGIN_PATH,
    DEFAULT_APP_PATH,
    API_ORIGIN,
    getSession: readStoredSession,
    hasValidSession,
    setSession,
    clearSession,
    ensureAuthenticated,
    redirectToLogin,
    redirectAfterLogin,
    applyUserProfile,
    applyRoleVisibility,
    getSessionRole,
    getSessionEmail,
    isEmployeeSession,
  };

  function refreshAuthenticatedUi() {
    if (!isLoginPage() && !isRootPage() && !ensureAuthenticated()) {
      return;
    }

    applyAuthenticatedUi();
  }

  window.addEventListener("pageshow", refreshAuthenticatedUi);
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") {
      refreshAuthenticatedUi();
    }
  });
  window.addEventListener("storage", (event) => {
    if (event.key === AUTH_STORAGE_KEY || event.key === null) {
      refreshAuthenticatedUi();
    }
  });

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", applyAuthenticatedUi, {
      once: true,
    });
  } else {
    applyAuthenticatedUi();
  }

  if (!isLoginPage() && !isRootPage()) {
    ensureAuthenticated();
  }
})();
