(function () {
  function validate(form) {
    if (!form.reportValidity()) return false;
    const values = new FormData(form);
    return Boolean(String(values.get("name") || "").trim() && String(values.get("reference") || "").trim());
  }

  window.SopmineDesign.ProductValidators = { validate };
})();
