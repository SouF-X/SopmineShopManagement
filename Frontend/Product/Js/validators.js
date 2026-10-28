(function () {
  const Design = window.SopmineDesign;

  function validate(form) {
    if (!form.reportValidity()) return false;
    const values = new FormData(form);
    const priceFields = ["purchase", "purchaseTtc", "saleHt", "saleTtc"];
    if (!priceFields.every((field) => Design.ProductMappers.isValidPrice(values.get(field)))) return false;
    return Boolean(String(values.get("name") || "").trim() && String(values.get("reference") || "").trim());
  }

  window.SopmineDesign.ProductValidators = { validate };
})();
