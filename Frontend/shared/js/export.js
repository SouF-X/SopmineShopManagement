(function () {
  const Design = window.SopmineDesign;

  function download(filename, headers, rows) {
    if (!rows.length) return false;
    const quote = (value) => `"${String(value ?? "").replaceAll('"', '""')}"`;
    const content = [headers, ...rows].map((row) => row.map(quote).join(";")).join("\r\n");
    const url = URL.createObjectURL(new Blob(["\ufeff", content], { type: "text/csv;charset=utf-8" }));
    const link = document.querySelector("#csv-download-link");
    link.href = url;
    link.download = filename;
    link.click();
    link.removeAttribute("href");
    link.removeAttribute("download");
    URL.revokeObjectURL(url);
    return true;
  }

  function current() {
    const route = Design.Router.current();
    let exported = false;
    const state = Design.Store.state;
    if (route.startsWith("products")) {
      exported = download("sopmine-produits.csv", ["Référence", "Produit", "Famille", "Unité", "Stock", "Minimum", "Achat HT", "Vente TTC"], state.products.map((item) => [item.reference, item.name, item.family, item.unit, item.quantity, item.minimum, item.purchase, item.sale]));
    } else if (route.startsWith("suppliers")) {
      exported = download("sopmine-fournisseurs.csv", ["Fournisseur", "ICE", "Ville", "Adresse", "Téléphone", "Email"], state.suppliers.map((item) => [item.name, item.ice, item.city, item.address, item.phone, item.email]));
    } else if (route.startsWith("clients")) {
      exported = download("sopmine-clients.csv", ["Client", "Type", "ICE", "Ville", "Adresse", "Téléphone"], state.clients.map((item) => [item.name, item.type, item.ice, item.city, item.address, item.phone]));
    } else {
      const isPurchase = route.startsWith("purchases");
      const isSale = route.startsWith("sales");
      if (!isPurchase && !isSale) return;
      const collection = isPurchase ? "purchases" : "sales";
      const key = route.split("/")[1] || (isPurchase ? "boncommande" : "devis");
      const section = Design.DocumentData.section(collection, key);
      const documents = state[collection].filter((item) => item.type === section.type);
      exported = download(`sopmine-${key}.csv`, ["Référence", "Date", "Échéance", "Statut", "Total TTC"], documents.map((item) => [item.ref, item.date, item.due, item.status, item.amount]));
    }
    Design.Components.toast(exported ? "Export prêt" : "Aucune donnée à exporter", exported ? "Le fichier CSV a été téléchargé." : "Ajoutez au moins un élément avant de lancer l’export.", exported ? "success" : "error");
  }

  Design.Export = { current };
})();
