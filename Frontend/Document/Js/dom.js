(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.DocumentDom = {
    page: () => Dom.view("documents-page-view"),
    nav: () => Dom.clone("document-nav-template"),
    row: () => Dom.clone("document-row-template"),
    detail: () => Dom.view("document-detail-view"),
    paperLine: () => Dom.clone("document-paper-line-template"),
    form: () => Dom.view("document-form-view"),
    aiWorkspace: () => Dom.view("invoice-ai-workspace-view"),
    line: () => Dom.clone("document-line-template"),
    deliveryAction: () => Dom.clone("delivery-conversion-action-template"),
    deliveryDialog: () => Dom.clone("delivery-conversion-dialog-template"),
    deliveryClientCard: () => Dom.clone("delivery-conversion-client-card-template"),
    deliveryRow: () => Dom.clone("delivery-conversion-row-template"),
    emptyRow: () => Dom.clone("document-empty-row-template"),
    flowStep: () => Dom.clone("document-flow-step-template"),
    missingAction: () => Dom.clone("document-missing-action-template"),
  };
})();
