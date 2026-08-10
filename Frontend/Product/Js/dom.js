(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.ProductDom = {
    page: () => Dom.view("products-page-view"),
    card: () => Dom.clone("product-card-template"),
    row: () => Dom.clone("product-row-template"),
    table: () => Dom.clone("product-table-template"),
    detail: () => Dom.view("product-detail-view"),
    form: () => Dom.view("product-form-view"),
  };
})();
