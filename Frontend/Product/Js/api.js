(function () {
  const Design = window.SopmineDesign;
  const base = Design.Api.endpoints.produits;

  Design.Api.products = {
    list: () => Design.Api.get(base),
    create: (payload) => Design.Api.send(base, "POST", payload),
    update: (id, payload) => Design.Api.send(`${base}/${id}`, "PUT", payload),
    remove: (id) => Design.Api.remove(`${base}/${id}`),
  };
})();
