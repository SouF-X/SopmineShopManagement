(function () {
  const Design = window.SopmineDesign;
  const base = Design.Api.endpoints.fournisseurs;

  Design.Api.suppliers = {
    list: () => Design.Api.get(base),
    create: (payload) => Design.Api.send(base, "POST", payload),
    update: (id, payload) => Design.Api.send(`${base}/${id}`, "PUT", payload),
    remove: (id) => Design.Api.remove(`${base}/${id}`),
    statement: (id, filters = {}) => Design.Api.get(`${base}/${id}/statement${Design.StatementPage?.queryString?.(filters) || ""}`),
  };
})();
