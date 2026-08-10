(function () {
  const Design = window.SopmineDesign;

  async function remove(supplier) {
    const confirmed = await Design.Components.confirmDelete({
      title: "Supprimer ce fournisseur ?",
      target: supplier.name,
      message: "Le fournisseur et ses contacts seront retirés de votre réseau d’achat.",
    });
    if (!confirmed) return;
    try {
      await Design.Api.suppliers.remove(supplier.id);
      await Design.WorkspacePage.reload();
      Design.Router.go("suppliers");
      Design.Components.toast("Fournisseur supprimé", "Le réseau d’achat a été mis à jour.");
    } catch (error) {
      Design.Components.toast("Suppression impossible", error.message, "error");
    }
  }

  Design.SupplierPage = {
    list: Design.SupplierList.showList,
    detail: Design.SupplierList.showDetail,
    form: Design.SupplierForms.render,
    remove,
  };
})();
