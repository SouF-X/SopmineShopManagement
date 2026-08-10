(function () {
  const Design = window.SopmineDesign;

  async function remove(documentItem, isPurchase) {
    const confirmed = await Design.Components.confirmDelete({
      title: "Supprimer ce document ?",
      target: documentItem.ref,
      message: "Le document et toutes ses lignes seront retirés de la liste correspondante.",
    });
    if (!confirmed) return;
    const collection = isPurchase ? "purchases" : "sales";
    const section = Design.DocumentData.sections[collection].find((item) => item.type === documentItem.type) || Design.DocumentData.sections[collection][0];
    try {
      await Design.Api.documents.remove(documentItem.id);
      await Design.WorkspacePage.reload();
      Design.Router.go(`${collection}/${section.key}`);
      Design.Components.toast("Document supprimé", "La liste a été mise à jour.");
    } catch (error) {
      Design.Components.toast("Suppression impossible", error.message, "error");
    }
  }

  Design.DocumentPage = {
    list: Design.DocumentList.showList,
    detail: Design.DocumentList.showDetail,
    form: Design.DocumentForms.render,
    aiWorkspace: Design.DocumentAiWorkspace.render,
    remove,
  };
})();
