(function () {
  const Design = window.SopmineDesign;

  function mapReference(dto, products, kind) {
    const name = String(dto.libelle || dto.Libelle || "Sans libellé");
    return {
      id: String(dto.id || dto.Id || ""),
      name,
      code: name.slice(0, 3).toUpperCase(),
      count: products.filter((product) => (kind === "family" ? product.family : product.unit) === name).length,
    };
  }

  Design.ReferenceMappers = { mapReference };
})();
