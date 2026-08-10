(function (root) {
  const PERIOD_DAYS = { today: 1, week: 7 };

  function startOfDay(value) {
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date;
  }

  function dateKey(date) {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  }

  function amount(value) { return Number(value || 0); }

  function compactMoney(value) {
    return new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 0 }).format(amount(value));
  }

  function periodDates(today, period) {
    const count = period === "month" ? today.getDate() : PERIOD_DAYS[period] || 7;
    return Array.from({ length: count }, (_, index) => {
      const date = new Date(today);
      date.setDate(today.getDate() - (count - 1 - index));
      return date;
    });
  }

  function buildBriefing({ periodRevenue, lowStockCount, receivables }) {
    if (!periodRevenue && !lowStockCount && !receivables) {
      return "Aucune vente facturée pour cette période. Le comptoir est prêt pour la prochaine opération.";
    }
    const parts = [periodRevenue ? `Les ventes atteignent ${compactMoney(periodRevenue)} MAD` : "Aucune vente n’est encore facturée"];
    if (lowStockCount) parts.push(`${lowStockCount} produit${lowStockCount > 1 ? "s demandent" : " demande"} votre attention`);
    if (receivables) parts.push(`${compactMoney(receivables)} MAD restent à encaisser`);
    return `${parts.join(". ")}.`;
  }

  function buildDashboard({ now = new Date(), products = [], suppliers = [], clients = [], purchases = [], sales = [] } = {}, period = "week") {
    const today = startOfDay(now);
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const clientNames = new Map(clients.map((client) => [client.id, client.name]));
    const supplierNames = new Map(suppliers.map((supplier) => [supplier.id, supplier.name]));
    const activeInvoices = sales.filter((document) => document.typeValue === 4 && document.statusValue !== 3);
    const monthInvoices = activeInvoices.filter((document) => new Date(`${document.dateValue}T00:00:00`) >= monthStart);
    const lowStock = products.filter((product) => amount(product.quantity) <= amount(product.minimum));
    const receivableInvoices = activeInvoices.filter((document) => amount(document.remainingAmount) > 0);
    const receivables = receivableInvoices.reduce((total, document) => total + amount(document.remainingAmount), 0);
    const latestSale = activeInvoices.slice().sort((left, right) => new Date(right.createdAt || right.dateValue) - new Date(left.createdAt || left.dateValue))[0];

    const salesTrend = periodDates(today, period).map((date) => {
      const iso = dateKey(date);
      const dayInvoices = activeInvoices.filter((document) => document.dateValue === iso);
      return { iso, date, value: dayInvoices.reduce((total, document) => total + amount(document.amount), 0), count: dayInvoices.length };
    });
    const periodRevenue = salesTrend.reduce((total, day) => total + day.value, 0);
    const periodInvoiceCount = salesTrend.reduce((total, day) => total + day.count, 0);
    const strongestDay = salesTrend.reduce((best, day) => day.value > (best?.value || 0) ? day : best, null);

    const alerts = [
      ...lowStock.filter((product) => amount(product.quantity) <= 0).map((product) => ({ kind: "out", priority: 1, product })),
      ...lowStock.filter((product) => amount(product.quantity) > 0).map((product) => ({ kind: "low", priority: 2, product })),
      ...(receivables > 0 ? [{ kind: "receivable", priority: 3, amount: receivables, count: receivableInvoices.length }] : []),
    ].sort((left, right) => left.priority - right.priority);

    const activity = [
      ...sales.map((document) => ({ kind: "sale", label: `${document.type || ["Devis", "", "", "Bon de livraison", "Facture client"][document.typeValue] || "Vente"} ${document.ref}`, detail: clientNames.get(document.partnerId) || "Client", amount: amount(document.amount), createdAt: document.createdAt || `${document.dateValue}T12:00:00`, route: `sale/${document.id}` })),
      ...purchases.map((document) => ({ kind: "purchase", label: `${document.type || "Document d’achat"} ${document.ref}`, detail: supplierNames.get(document.partnerId) || "Fournisseur", amount: amount(document.amount), createdAt: document.createdAt || `${document.dateValue}T12:00:00`, route: `purchase/${document.id}` })),
      ...products.filter((product) => product.createdAt).map((product) => ({ kind: "product", label: `Produit ${product.reference}`, detail: product.name, createdAt: product.createdAt, route: `product/${product.id}` })),
    ].sort((left, right) => new Date(right.createdAt) - new Date(left.createdAt)).slice(0, 7);

    return {
      period,
      briefing: buildBriefing({ periodRevenue, lowStockCount: lowStock.length, receivables }),
      periodRevenue,
      periodInvoiceCount,
      strongestDay: strongestDay?.value ? strongestDay : null,
      monthRevenue: monthInvoices.reduce((total, document) => total + amount(document.amount), 0),
      receivables,
      receivableCount: receivableInvoices.length,
      stockValue: products.reduce((total, product) => total + amount(product.quantity) * amount(product.purchase), 0),
      lowStockCount: lowStock.length,
      latestSale: latestSale ? { ...latestSale, partnerName: clientNames.get(latestSale.partnerId) || "Client" } : null,
      salesTrend,
      alerts,
      activity,
      counts: { products: products.length, clients: clients.length, suppliers: suppliers.length, sales: sales.length },
    };
  }

  root.SopmineDashboardModel = { buildDashboard };
})(globalThis);