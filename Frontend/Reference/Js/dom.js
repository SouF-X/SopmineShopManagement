(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.ReferenceDom = {
    page: () => Dom.view("references-page-view"),
    panel: () => Dom.clone("reference-panel-template"),
    row: () => Dom.clone("reference-row-template"),
    formDialog: () => Dom.clone("reference-form-dialog-template"),
  };
})();
