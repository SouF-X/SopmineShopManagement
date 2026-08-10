(function () {
  const Design = window.SopmineDesign;
  const typeNames = { devis: 0, boncommande: 1, bonreception: 2, bonlivraison: 3, facture: 4, avoir: 5 };
  const natureNames = { achat: 0, vente: 1 };
  const statusNames = { draft: 0, validated: 1, paid: 2, cancelled: 3 };
  const paymentNames = { nonpayee: 0, payee: 1 };
  const methodNames = { espece: 0, cheque: 1, virement: 2, effet: 3, carte: 4 };
  const progressNames = { unpaid: "unpaid", nonreglee: "unpaid", partiallypaid: "partial", partial: "partial", partiellementreglee: "partial", paid: "paid", reglee: "paid", overdue: "overdue", enretard: "overdue" };

  function enumValue(value, names, fallback = 0) {
    if (typeof value === "number") return value;
    if (/^\d+$/.test(String(value || ""))) return Number(value);
    const key = String(value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    return names[key] ?? fallback;
  }

  function typeLabel(type, nature) {
    if (type === 4) return nature === 0 ? "Facture fournisseur" : "Facture client";
    if (type === 5) return nature === 0 ? "Avoir fournisseur" : "Avoir client";
    return ["Devis", "Bon de commande", "Bon de réception", "Bon de livraison"][type] || "Document";
  }

  function mapDocument(dto) {
    const typeValue = enumValue(dto.type ?? dto.Type, typeNames);
    const natureValue = enumValue(dto.nature ?? dto.Nature, natureNames);
    const statusValue = enumValue(dto.status ?? dto.Status, statusNames);
    const paymentRaw = dto.paymentStatus ?? dto.PaymentStatus;
    const methodRaw = dto.paymentMethod ?? dto.PaymentMethod;
    const lines = (dto.lines || dto.Lines || [])
      .slice()
      .sort((a, b) => Number(a.lineOrder ?? a.LineOrder ?? 0) - Number(b.lineOrder ?? b.LineOrder ?? 0))
      .map((line) => ({
        id: String(line.invoiceLineId || line.InvoiceLineId || ""),
        productId: line.produitId || line.ProduitId || null,
        product: String(line.productName || line.ProductName || "Article"),
        ref: String(line.productReference || line.ProductReference || ""),
        family: String(line.productFamily || line.ProductFamily || ""),
        productUnit: String(line.productUnit || line.ProductUnit || ""),
        qty: Number(line.quantity ?? line.Quantity ?? 0),
        unit: Number(line.price ?? line.Price ?? 0),
        priceTtc: Number(line.priceTTC ?? line.PriceTTC ?? 0),
        vat: Number(line.tva ?? line.TVA ?? 20),
      }));
    return {
      id: String(dto.invoiceId || dto.InvoiceId || ""),
      createdAt: dto.createdAtUtc || dto.CreatedAtUtc || null,
      ref: String(dto.reference || dto.Reference || "—"),
      typeValue,
      natureValue,
      type: typeLabel(typeValue, natureValue),
      dateValue: Design.Utils.isoDate(dto.date || dto.Date),
      dueValue: Design.Utils.isoDate(dto.dueDate || dto.DueDate),
      date: Design.Utils.formatDate(dto.date || dto.Date),
      due: Design.Utils.formatDate(dto.dueDate || dto.DueDate),
      partnerId: natureValue === 0 ? dto.fournisseurId || dto.FournisseurId : dto.clientId || dto.ClientId,
      statusValue,
      status: ["Brouillon", "Validé", "Payé", "Annulé"][statusValue] || "Brouillon",
      paymentStatusValue: paymentRaw == null ? null : enumValue(paymentRaw, paymentNames),
      paymentStatus: paymentRaw == null ? null : enumValue(paymentRaw, paymentNames) === 1 ? "Payée" : "Non payée",
      paymentMethodValue: methodRaw == null ? null : enumValue(methodRaw, methodNames),
      paymentMethod: methodRaw == null ? null : ["Espèce", "Chèque", "Virement", "Effet", "Carte"][enumValue(methodRaw, methodNames)],
      totalPaid: Number(dto.totalPaid ?? dto.TotalPaid ?? 0),
      remainingAmount: Math.max(0, Number(dto.total ?? dto.Total ?? 0) - Number(dto.totalPaid ?? dto.TotalPaid ?? 0)),
      paymentProgress: normalizeProgress(dto.paymentProgress ?? dto.PaymentProgress),
      notes: dto.notes || dto.Notes || "",
      subtotal: Number(dto.subtotal ?? dto.Subtotal ?? 0),
      taxTotal: Number(dto.taxTotal ?? dto.TaxTotal ?? 0),
      amount: Number(dto.total ?? dto.Total ?? 0),
      convertedToInvoiceId: dto.convertedToInvoiceId ?? dto.ConvertedToInvoiceId ?? null,
      lines,
    };
  }

  function normalizeProgress(value) {
    if (typeof value === "number") return ["unpaid", "partial", "paid", "overdue"][value] || "unpaid";
    const key = String(value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    return progressNames[key] || "unpaid";
  }

  function linesForPresentation(lines) {
    return (lines || [])
      .map((line, index) => ({ line, index, family: String(line?.family || "").trim() }))
      .sort((a, b) => {
        if (!a.family && !b.family) return a.index - b.index;
        if (!a.family) return 1;
        if (!b.family) return -1;
        return a.family.localeCompare(b.family, "fr", { sensitivity: "base" }) || a.index - b.index;
      })
      .map(({ line }) => line);
  }

  function toPayload(form, context) {
    const values = new FormData(form);
    const lines = [...form.querySelectorAll("[data-line]")].map((row) => {
      const price = Design.Utils.number(row.querySelector("[data-line-price]").value);
      const vat = Design.Utils.number(row.querySelector("[data-line-vat]").value);
      const priceTTC = context.isPurchase
        ? Number((price * (1 + vat / 100)).toFixed(2))
        : Design.Utils.number(row.querySelector("[data-line-price-ttc]").value);
      return {
        invoiceLineId: Design.Utils.optional(row.dataset.lineId),
        produitId: context.isPurchase && context.catalogueMode === false ? null : Design.Utils.optional(row.querySelector("[data-line-product]").value),
        productReference: Design.Utils.optional(row.dataset.productReference),
        productName: Design.Utils.optional(row.dataset.productName),
        productFamily: Design.Utils.optional(row.querySelector("[data-line-family]")?.value ?? row.dataset.productFamily),
        productUnit: Design.Utils.optional(row.dataset.productUnit),
        quantity: Design.Utils.number(row.querySelector("[data-line-quantity]").value),
        price,
        priceTTC,
        tva: vat,
      };
    });
    const archiveOnly = context.isPurchase && context.catalogueMode === false;
    const partnerId = String(values.get("partner") || "");
    const createExtractedSupplier = context.isPurchase
      && !archiveOnly
      && partnerId === "__new_supplier__"
      && context.extraction?.supplier;
    return {
      nature: context.isPurchase ? 0 : 1,
      type: context.typeValue,
      status: Number(values.get("status") || 0),
      reference: Design.Utils.optional(values.get("reference")),
      date: values.get("date"),
      dueDate: Design.Utils.optional(values.get("dueDate")),
      fournisseurId: context.isPurchase && !createExtractedSupplier ? Design.Utils.optional(partnerId) : null,
      clientId: context.isPurchase ? null : partnerId,
      newSupplier: archiveOnly ? null : (createExtractedSupplier ? {
        name: Design.Utils.optional(context.extraction.supplier.name),
        ice: Design.Utils.optional(context.extraction.supplier.ice),
        address: Design.Utils.optional(context.extraction.supplier.address),
        city: Design.Utils.optional(context.extraction.supplier.city),
        phone: Design.Utils.optional(context.extraction.supplier.phone),
        email: Design.Utils.optional(context.extraction.supplier.email),
        website: Design.Utils.optional(context.extraction.supplier.website),
      } : null),
      notes: Design.Utils.optional(values.get("notes")),
      total: Number(lines.reduce((sum, line) => {
        if (!context.isPurchase) {
          const lineTtc = Number((line.quantity * line.priceTTC).toFixed(2));
          return sum + lineTtc;
        }
        const subtotal = Number((line.quantity * line.price).toFixed(2));
        const tax = Number((subtotal * line.tva / 100).toFixed(2));
        return sum + subtotal + tax;
      }, 0).toFixed(2)),
      catalogueMode: context.catalogueMode !== false,
      lines,
    };
  }

  Design.DocumentMappers = { mapDocument, toPayload, linesForPresentation };
})();
