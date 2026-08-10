(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.SupplierDom = {
    page: () => Dom.view("suppliers-page-view"),
    card: () => Dom.clone("supplier-card-template"),
    detail: () => Dom.view("supplier-detail-view"),
    form: () => Dom.view("supplier-form-view"),
    contact: () => Dom.clone("contact-entry-template"),
    contactCard: () => Dom.clone("supplier-contact-template"),
    productLink: () => Dom.clone("supplier-product-link-template"),
  };
})();
