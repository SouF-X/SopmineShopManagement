(function () {
  const Design = window.SopmineDesign;

  async function remove(product) {
    const confirmed = await Design.Components.confirmDelete({
      title: "Supprimer ce produit ?",
      target: product.name,
      message: "Le produit sera retiré du catalogue et ne sera plus disponible dans les sélections.",
    });
    if (!confirmed) return;
    try {
      await Design.Api.products.remove(product.id);
      await Design.WorkspacePage.reload();
      Design.Router.go("products");
      Design.Components.toast("Produit supprimé", "Le catalogue a été mis à jour.");
    } catch (error) {
      Design.Components.toast("Suppression impossible", error.message, "error");
    }
  }

  Design.ProductPage = {
    list: Design.ProductList.showList,
    detail: Design.ProductList.showDetail,
    form: Design.ProductForms.render,
    remove,
  };
})();
