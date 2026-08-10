(function () {
  const Design = window.SopmineDesign;
  const client = window.SopmineApi;

  if (!client) throw new Error("Le client API Sopmine est indisponible.");

  function errorMessage(data, fallback) {
    return window.SopmineFeedback?.getErrorDetails?.(data, fallback)?.message
      || data?.description
      || data?.detail
      || data?.title
      || fallback;
  }

  async function request(url, options = {}) {
    const { response, data } = await client.requestJson(url, options);
    if (!response.ok) throw new Error(errorMessage(data, "L’opération API a échoué."));
    return data;
  }

  function get(url) {
    return request(url);
  }

  function send(url, method, payload) {
    return request(url, {
      method,
      headers: { "Content-Type": "application/json" },
      body: payload === undefined ? undefined : JSON.stringify(payload),
    });
  }

  function remove(url) {
    return request(url, { method: "DELETE" });
  }

  function upload(url, formData, options = {}) {
    return request(url, { ...options, method: "POST", body: formData });
  }

  Design.Api = {
    endpoints: client.endpoints,
    get,
    send,
    remove,
    upload,
  };
})();
