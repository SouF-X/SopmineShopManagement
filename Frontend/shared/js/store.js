(function () {
  const Design = window.SopmineDesign;

  const state = {
    products: [],
    suppliers: [],
    clients: [],
    purchases: [],
    sales: [],
    families: [],
    units: [],
    productView: "grid",
    selectedClientId: null,
    pendingPartnerId: null,
    pendingDocumentPartnerId: null,
    ready: false,
  };

  function setWorkspace(data) {
    state.products = data.products;
    state.suppliers = data.suppliers;
    state.clients = data.clients;
    state.purchases = data.invoices.filter((item) => item.natureValue === 0).sort(newestDocumentFirst);
    state.sales = data.invoices.filter((item) => item.natureValue === 1).sort(newestDocumentFirst);
    const openDocumentsByClient = new Map();
    state.sales.forEach((document) => {
      if (document.statusValue >= 2 || !document.partnerId) return;
      openDocumentsByClient.set(
        document.partnerId,
        (openDocumentsByClient.get(document.partnerId) || 0) + 1,
      );
    });
    state.clients.forEach((client) => {
      client.openDocs = openDocumentsByClient.get(client.id) || 0;
    });
    state.families = data.families;
    state.units = data.units;
    if (!state.clients.some((client) => client.id === state.selectedClientId)) {
      state.selectedClientId = state.clients[0]?.id || null;
    }
    state.ready = true;
  }

  function newestDocumentFirst(left, right) {
    const dateDifference = new Date(`${right.dateValue || "1970-01-01"}T00:00:00`).getTime()
      - new Date(`${left.dateValue || "1970-01-01"}T00:00:00`).getTime();
    if (dateDifference) return dateDifference;
    const createdDifference = new Date(right.createdAt || 0).getTime() - new Date(left.createdAt || 0).getTime();
    if (createdDifference) return createdDifference;
    return right.ref.localeCompare(left.ref, "fr", { numeric: true });
  }

  const byId = {
    product: (id) => state.products.find((item) => item.id === id),
    supplier: (id) => state.suppliers.find((item) => item.id === id),
    client: (id) => state.clients.find((item) => item.id === id),
    purchase: (id) => state.purchases.find((item) => item.id === id),
    sale: (id) => state.sales.find((item) => item.id === id),
  };

  Design.Store = { state, setWorkspace, byId };
})();
