(function () {
  const sections = {
    purchases: [
      { key: "boncommande", typeValue: 1, type: "Bon de commande", title: "Bons de commande", singular: "bon de commande", action: "Nouveau bon de commande", icon: "shopping_cart", description: "Préparez et suivez uniquement les commandes adressées aux fournisseurs." },
      { key: "bonreception", typeValue: 2, type: "Bon de réception", title: "Bons de réception", singular: "bon de réception", action: "Nouveau bon de réception", icon: "inventory", description: "Contrôlez séparément les marchandises réceptionnées." },
      { key: "facture", typeValue: 4, type: "Facture fournisseur", title: "Factures fournisseurs", singular: "facture fournisseur", action: "Nouvelle facture fournisseur", icon: "receipt_long", description: "Consultez les factures d’achat, échéances et règlements." },
      { key: "avoir", typeValue: 5, type: "Avoir fournisseur", title: "Avoirs fournisseurs", singular: "avoir fournisseur", action: "Nouvel avoir fournisseur", icon: "assignment_return", description: "Retrouvez les avoirs fournisseurs séparément." },
    ],
    sales: [
      { key: "devis", typeValue: 0, type: "Devis", title: "Devis", singular: "devis", action: "Nouveau devis", icon: "request_quote", description: "Affichez uniquement les propositions commerciales envoyées aux clients." },
      { key: "bonlivraison", typeValue: 3, type: "Bon de livraison", title: "Bons de livraison", singular: "bon de livraison", action: "Nouveau bon de livraison", icon: "local_shipping", description: "Suivez séparément les documents de livraison." },
      { key: "facture", typeValue: 4, type: "Facture client", title: "Factures clients", singular: "facture client", action: "Nouvelle facture client", icon: "receipt_long", description: "Consultez les factures de vente, échéances et règlements." },
      { key: "avoir", typeValue: 5, type: "Avoir client", title: "Avoirs clients", singular: "avoir client", action: "Nouvel avoir client", icon: "assignment_return", description: "Isolez les avoirs clients pour un suivi clair." },
    ],
  };

  const aiInvoiceWorkspace = {
    key: "lecture-ia",
    title: "Lecture IA",
    action: "Importer un document fournisseur",
    icon: "document_scanner",
    description: "Importez un document fournisseur et vérifiez les données extraites avant confirmation.",
  };

  function section(kind, key) {
    return sections[kind].find((item) => item.key === key) || sections[kind][0];
  }

  window.SopmineDesign.DocumentData = { sections, aiInvoiceWorkspace, section };
})();
