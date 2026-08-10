(function () {
  const Design = window.SopmineDesign;
  let loadingPromise = null;

  async function load() {
    Design.Shell.setApiStatus("loading", "Synchronisation…");
    const [productDtos, supplierDtos, clientDtos, documentDtos, familyDtos, unitDtos] = await Promise.all([
      Design.Api.products.list(),
      Design.Api.suppliers.list(),
      Design.Api.clients.list(),
      Design.Api.documents.list(),
      Design.Api.families.list(),
      Design.Api.units.list(),
    ]);
    const products = (productDtos || []).map(Design.ProductMappers.mapProduct)
      .sort((a, b) => new Date(b.createdAt || 0) - new Date(a.createdAt || 0));
    const data = {
      products,
      suppliers: (supplierDtos || []).map(Design.SupplierMappers.mapSupplier)
        .sort((a, b) => new Date(b.createdAt || 0) - new Date(a.createdAt || 0)),
      clients: (clientDtos || []).map(Design.ClientMappers.mapClient),
      invoices: (documentDtos || []).map(Design.DocumentMappers.mapDocument),
      families: (familyDtos || []).map((item) => Design.ReferenceMappers.mapReference(item, products, "family")),
      units: (unitDtos || []).map((item) => Design.ReferenceMappers.mapReference(item, products, "unit")),
    };
    Design.Store.setWorkspace(data);
    Design.Shell.updateCounts();
    Design.Shell.setApiStatus("online", "Connecté");
  }

  function reload() {
    if (!loadingPromise) {
      loadingPromise = load().finally(() => { loadingPromise = null; });
    }
    return loadingPromise;
  }

  async function finalizeMutation({
    refresh = reload,
    successTitle,
    successMessage,
    onRefreshed,
    onRefreshFailed,
  }) {
    Design.Components.toast(successTitle, successMessage);

    try {
      await refresh();
      onRefreshed?.();
      return { refreshed: true };
    } catch (error) {
      onRefreshFailed?.();
      Design.Components.toast(
        "Enregistré, actualisation incomplète",
        `${error?.message || "La synchronisation a échoué."} Utilisez Réessayer avant de poursuivre.`,
        "warning",
      );
      return { refreshed: false, error };
    }
  }

  Design.WorkspacePage = { load: reload, reload, finalizeMutation };
})();
