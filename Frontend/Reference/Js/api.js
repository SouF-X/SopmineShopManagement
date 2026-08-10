(function () {
  const Design = window.SopmineDesign;

  function resource(endpoint) {
    return {
      list: () => Design.Api.get(endpoint),
      create: (libelle) => Design.Api.send(endpoint, "POST", { libelle }),
      update: (id, libelle) => Design.Api.send(`${endpoint}/${id}`, "PUT", { libelle }),
      remove: (id) => Design.Api.remove(`${endpoint}/${id}`),
    };
  }

  Design.Api.families = resource(Design.Api.endpoints.familles);
  Design.Api.units = resource(Design.Api.endpoints.unitesMesure);
})();
