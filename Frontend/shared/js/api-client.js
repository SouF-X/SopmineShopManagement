(() => {
  // Central API configuration. Page scripts should read URLs from here instead
  // of hardcoding host-specific values or repeating endpoint strings.
  const runtimeConfig = window.SopmineRuntimeConfig || {};
  const configuredApiBase = normalizeUrl(runtimeConfig.apiBaseUrl);
  const configuredApiOrigin = normalizeOrigin(runtimeConfig.apiOrigin);
  const defaultApiOrigin = getDefaultApiOrigin();
  const API_BASE = configuredApiBase || `${configuredApiOrigin || defaultApiOrigin}/api/v1`;
  const API_ORIGIN = new URL(API_BASE).origin;
  const GET_CACHE_PREFIX = "sopmine-api-cache:";
  const GET_CACHE_TTL_MS = 45 * 1000;
  const DEFAULT_REQUEST_TIMEOUT_MS = 15 * 1000;
  const AUTH_STORAGE_KEY = "sopmine-auth-session";

  const endpoints = Object.freeze({
    login: `${API_BASE}/auth/login`,
    clients: `${API_BASE}/clients`,
    fournisseurs: `${API_BASE}/fournisseurs`,
    invoices: `${API_BASE}/invoices`,
    invoiceExtraction: `${API_BASE}/achats/extract-from-image`,
    produits: `${API_BASE}/produits`,
    familles: `${API_BASE}/familles`,
    unitesMesure: `${API_BASE}/unites-mesure`,
    documentNominations: `${API_BASE}/document-nominations`,
    users: `${API_BASE}/users`,
  });

  function getDefaultApiOrigin() {
    if (window.location.origin && window.location.origin !== "null") {
      return window.location.origin;
    }

    return "http://localhost:5269";
  }

  function normalizeOrigin(value) {
    const url = normalizeUrl(value);
    return url ? new URL(url).origin : "";
  }

  function normalizeUrl(value) {
    const text = typeof value === "string" ? value.trim() : "";

    if (!text) {
      return "";
    }

    try {
      return new URL(text, window.location.origin).href.replace(/\/$/, "");
    } catch {
      console.warn(`Ignoring invalid Sopmine API URL: ${text}`);
      return "";
    }
  }

  function normalizeMethod(options) {
    return (options.method || "GET").toUpperCase();
  }

  function getCacheScope() {
    try {
      const session = JSON.parse(window.localStorage.getItem(AUTH_STORAGE_KEY) || "{}");
      const token = typeof session?.accessToken === "string"
        ? session.accessToken.trim()
        : "";

      return token ? token.slice(-32) : "anonymous";
    } catch {
      return "anonymous";
    }
  }

  function getCacheKey(url) {
    return `${GET_CACHE_PREFIX}${getCacheScope()}:${new URL(url, window.location.origin).href}`;
  }

  function isCacheableGet(url, options) {
    if (normalizeMethod(options) !== "GET") {
      return false;
    }

    const requestUrl = new URL(url, window.location.origin);
    return requestUrl.href.startsWith(`${API_BASE}/`);
  }

  function createCachedResponse(data = null) {
    return new Response(data === null ? null : JSON.stringify(data), {
      status: 200,
      statusText: "OK",
      headers: {
        "content-type": "application/json",
        "x-sopmine-cache": "hit",
      },
    });
  }

  function readCachedJson(url) {
    try {
      const cached = window.sessionStorage.getItem(getCacheKey(url));

      if (!cached) {
        return null;
      }

      const entry = JSON.parse(cached);

      if (!entry || Date.now() - entry.cachedAt > GET_CACHE_TTL_MS) {
        window.sessionStorage.removeItem(getCacheKey(url));
        return null;
      }

      return entry.data;
    } catch {
      return null;
    }
  }

  function writeCachedJson(url, data) {
    try {
      window.sessionStorage.setItem(
        getCacheKey(url),
        JSON.stringify({
          cachedAt: Date.now(),
          data,
        }),
      );
    } catch {
      // Browser storage can be full or unavailable; API calls should still work.
    }
  }

  function clearCachedGets() {
    try {
      for (let index = window.sessionStorage.length - 1; index >= 0; index -= 1) {
        const key = window.sessionStorage.key(index);

        if (key?.startsWith(GET_CACHE_PREFIX)) {
          window.sessionStorage.removeItem(key);
        }
      }
    } catch {
      // Ignore storage failures and let the next network request refill cache.
    }
  }

  // Small helper for API calls that return JSON. It keeps the original response
  // available so page scripts can still check response.ok or status codes.
  async function requestJson(url, options = {}) {
    if (isCacheableGet(url, options)) {
      const cachedData = readCachedJson(url);

      if (cachedData !== null) {
        return { response: createCachedResponse(cachedData), data: cachedData };
      }
    }

    const {
      timeoutMs = DEFAULT_REQUEST_TIMEOUT_MS,
      signal: callerSignal,
      ...fetchOptions
    } = options;
    const controller = new AbortController();
    const resolvedTimeout = Number.isFinite(Number(timeoutMs)) && Number(timeoutMs) > 0
      ? Number(timeoutMs)
      : DEFAULT_REQUEST_TIMEOUT_MS;
    let timedOut = false;
    const abortFromCaller = () => controller.abort(callerSignal?.reason);
    const timeoutId = setTimeout(() => {
      timedOut = true;
      controller.abort();
    }, resolvedTimeout);

    if (callerSignal?.aborted) {
      abortFromCaller();
    } else {
      callerSignal?.addEventListener("abort", abortFromCaller, { once: true });
    }

    let response;

    try {
      response = await fetch(url, {
        ...fetchOptions,
        signal: controller.signal,
      });
    } catch (error) {
      if (timedOut) {
        const timeoutError = new Error("La requête a expiré. Réessayez.");
        timeoutError.name = "TimeoutError";
        throw timeoutError;
      }

      throw error;
    } finally {
      clearTimeout(timeoutId);
      callerSignal?.removeEventListener("abort", abortFromCaller);
    }

    const data = await response.json().catch(() => null);

    if (isCacheableGet(url, options) && response.ok) {
      writeCachedJson(url, data);
    } else if (normalizeMethod(options) !== "GET" && response.ok) {
      clearCachedGets();
    }

    return { response, data };
  }

  window.SopmineApi = Object.freeze({
    origin: API_ORIGIN,
    baseUrl: API_BASE,
    endpoints,
    requestJson,
    isCacheableGet,
    readCachedJson,
    writeCachedJson,
    createCachedResponse,
    clearCachedGets,
  });
})();
