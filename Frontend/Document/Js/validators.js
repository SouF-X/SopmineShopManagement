(function () {
  const Design = window.SopmineDesign;

  function validate(form) {
    if (!form.reportValidity()) return false;
    const lines = [...form.querySelectorAll("[data-line]")];
    if (!lines.length) {
      Design.Components.toast("Articles requis", "Ajoutez au moins une ligne.", "error");
      return false;
    }
    const invalid = lines.some((row) => {
      const productId = row.querySelector("[data-line-product]").value;
      const productName = row.dataset.productName;
      const quantity = Design.Utils.number(row.querySelector("[data-line-quantity]").value);
      return (!productId && !productName) || quantity <= 0;
    });
    if (invalid) {
      Design.Components.toast("Articles incomplets", "Chaque ligne doit avoir un produit et une quantité positive.", "error");
      return false;
    }
    return true;
  }

  Design.DocumentValidators = { validate };
})();
