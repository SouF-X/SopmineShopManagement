(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.ClientDom = {
    page: () => Dom.view("clients-page-view"),
    row: () => Dom.clone("client-row-template"),
    preview: () => Dom.clone("client-preview-template"),
    detail: () => Dom.view("client-detail-view"),
    form: () => Dom.view("client-form-view"),
    contact: () => Dom.clone("contact-entry-template"),
    emptyAction: () => Dom.clone("client-empty-action-template"),
    contactCard: () => Dom.clone("client-contact-card-template"),
    documentLink: () => Dom.clone("client-document-link-template"),
  };
})();
