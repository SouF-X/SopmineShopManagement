(function () {
  const Design = window.SopmineDesign;

  async function remove(client) {
    const confirmed = await Design.Components.confirmDelete({
      title: "Supprimer ce client ?",
      target: client.name,
      message: "Le client et ses contacts seront retirés du portefeuille commercial.",
    });
    if (!confirmed) return;
    try {
      await Design.Api.clients.remove(client.id);
      await Design.WorkspacePage.reload();
      Design.Router.go("clients");
      Design.Components.toast("Client supprimé", "Le portefeuille a été mis à jour.");
    } catch (error) {
      Design.Components.toast("Suppression impossible", error.message, "error");
    }
  }

  Design.ClientPage = {
    list: Design.ClientList.showList,
    detail: Design.ClientList.showDetail,
    form: Design.ClientForms.render,
    remove,
  };
})();
