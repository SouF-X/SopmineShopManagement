(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const U = Design.Utils;
  const Data = Design.DocumentData;

  function setFormStatus(select, statusValue) {
    const value = String(statusValue ?? 0);
    const isPaid = value === "2";
    let paidOption = select.querySelector('option[value="2"]');
    if (isPaid && !paidOption) {
      paidOption = new Option("Pay\u{e9}", "2");
      select.add(paidOption);
    }
    if (!isPaid) paidOption?.remove();
    select.value = value;
    select.disabled = isPaid;
  }
  function build(kind, key, id, { extraction = null, embedded = false, catalogueMode = true, requireExistingPartner = false } = {}) {
    const isPurchase = kind === "purchase";
    const collection = isPurchase ? "purchases" : "sales";
    const existing = id ? (isPurchase ? Store.byId.purchase(id) : Store.byId.sale(id)) : null;
    if (id && !existing) return Design.Shell.missing("Ce document n\u{2019}existe plus", `${collection}/${key || (isPurchase ? "boncommande" : "devis")}`);
    const fallback = Data.section(collection, key);
    const section = existing ? Data.sections[collection].find((item) => item.type === existing.type) || fallback : fallback;
    const isDeliveryNote = !isPurchase && section.key === "bonlivraison";
    const partners = isPurchase ? Store.state.suppliers : Store.state.clients;
    const requiresExistingPartner = isPurchase && requireExistingPartner;
    if (catalogueMode && ((!Store.state.products.length && !extraction?.lines?.length) || (!partners.length && !extraction?.supplier?.name))) {
      return renderMissingPrerequisite(isPurchase, section);
    }
    const page = Design.DocumentDom.form();
    page.classList.toggle("delivery-fast-form", isDeliveryNote);
    page.dataset.documentType = section.key || "";
    const form = page.querySelector("[data-document-form]");
    configureDeliveryFastFlow(form, isDeliveryNote);
    const listRoute = `${collection}/${section.key}`;
    page.querySelectorAll("[data-back]").forEach((button) => button.dataset.route = existing ? `${isPurchase ? "purchase" : "sale"}/${id}` : listRoute);
    Design.Dom.setText(page, "[data-form-icon]", section.icon);
    Design.Dom.setText(page, "[data-form-eyebrow]", isPurchase ? " Document d\u{2019}achat" : " Document de vente");
    Design.Dom.setText(page, "[data-form-title]", existing ? `Modifier ${existing.ref}` : section.action);
    Design.Dom.setText(page, "[data-form-subtitle]", isDeliveryNote
      ? "S\u{e9}lectionnez le client et ajoutez les quantit\u{e9}s livr\u{e9}es."
      : "Saisissez les articles et contr\u{f4}lez les totaux rapidement.");
    Design.Dom.setText(page, "[data-lines-helper]", isDeliveryNote
      ? "Produits et quantit\u{e9}s livr\u{e9}es"
      : "Quantit\u{e9}s, prix unitaires et taxes");
    Design.Dom.setText(page, "[data-add-line-help]", isDeliveryNote
      ? "Recherchez un produit puis indiquez la quantit\u{e9} livr\u{e9}e."
      : "S\u{e9}lectionnez un produit puis ajustez quantit\u{e9}, prix et taxes.");
    page.querySelectorAll("[data-submit-label]").forEach((node) => { node.textContent = existing ? "Enregistrer" : "Cr\u{e9}er le document"; });
    Design.Dom.setText(page, "[data-partner-title]", `${isPurchase ? "Fournisseur" : "Client"} et conditions`);
    Design.Dom.setText(page, "[data-partner-label]", isPurchase ? "Fournisseur" : "Client");

    const extractedPartner = (catalogueMode || requiresExistingPartner)
      ? Design.DocumentExtraction.matchSupplier(extraction)
      : null;
    const preferredPartner = (catalogueMode || requiresExistingPartner)
      ? (existing?.partnerId || extractedPartner?.id || Store.state.pendingPartnerId)
      : null;
    const partnerSelect = form.elements.partner;
    const partnerOptions = requiresExistingPartner
      ? extractedPartner
        ? [new Option(extractedPartner.name, extractedPartner.id, true, true)]
        : [new Option("Fournisseur introuvable \u{2014} ajoutez-le d\u{2019}abord", "", true, true)]
      : catalogueMode
        ? partners.map((partner) => new Option(partner.name, partner.id, false, partner.id === preferredPartner))
        : [new Option(extraction?.supplier?.name || "Fournisseur extrait", "", true, true)];
    if (catalogueMode && extraction && !extractedPartner) {
      partnerOptions.unshift(new Option("Nouveau \u{b7} " + (extraction.supplier.name || "Fournisseur d\u{e9}tect\u{e9}"), "__new_supplier__", true, true));
    }
    partnerSelect.replaceChildren(...partnerOptions);
    partnerSelect.required = catalogueMode || requiresExistingPartner;
    partnerSelect.disabled = !catalogueMode && !requiresExistingPartner;
    Store.state.pendingPartnerId = null;
    const dates = U.todayAndDue();
    form.elements.date.value = existing?.dateValue || extraction?.dateValue || dates.today;
    form.elements.dueDate.value = existing?.dueValue || dates.due;
    form.elements.reference.value = existing?.ref || extraction?.reference || "";
    setFormStatus(form.elements.status, existing?.statusValue ?? 0);
    form.elements.notes.value = existing?.notes || "";
    const autoGenerateReference = !existing && !extraction;
    if (autoGenerateReference) {
      form.elements.reference.placeholder = "R\u{e9}f\u{e9}rence sugg\u{e9}r\u{e9}e automatiquement";
      prefillSuggestedReference(form, { isPurchase, section });
    }

    const lineHost = page.querySelector("[data-line-list]");
    if (existing?.lines.length) {
      lineHost.replaceChildren(...existing.lines.map((line) => lineRow(line, isPurchase, { catalogueMode })));
    } else if (extraction?.lines.length) {
      lineHost.replaceChildren(...extraction.lines.map((line) => {
        const product = catalogueMode ? Design.DocumentExtraction.matchProduct(line) : null;
        return lineRow({ ...line, productId: product?.id || null }, isPurchase, { catalogueMode });
      }));
    } else {
      lineHost.appendChild(lineRow({
        productId: catalogueMode ? Store.state.products[0]?.id : null,
        productName: catalogueMode ? "" : "Article extrait",
        qty: 1,
      }, isPurchase, { catalogueMode }));
    }
    const syncLineFields = showAllLineFields(page, isPurchase);
    page.querySelector("[data-add-line]").addEventListener("click", () => {
      const row = lineRow({
        productId: catalogueMode ? Store.state.products[0]?.id : null,
        productName: catalogueMode ? "" : "Article ajoute",
        qty: 1,
      }, isPurchase, { catalogueMode });
      lineHost.appendChild(row);
      syncLineFields();
      updateTotals(page);
      requestAnimationFrame(() => row.querySelector("[data-line-product]")?.focus());
    });
    if (isDeliveryNote) {
      const prepareFastNumberEntry = (event) => {
        if (!window.matchMedia("(max-width: 1180px)").matches) return;
        const input = event.target.closest("[data-line-quantity], [data-line-price-ttc]");
        if (!input || !form.contains(input)) return;
        try {
          input.select();
        } catch {
          input.value = "";
          input.dispatchEvent(new Event("input", { bubbles: true }));
        }
      };
      form.addEventListener("pointerdown", prepareFastNumberEntry);
      form.addEventListener("focusin", prepareFastNumberEntry);
    }
    form.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-line]");
      if (!button) return;
      if (lineHost.querySelectorAll("[data-line]").length <= 1) {
        Design.Components.toast("Une ligne requise", "Le document doit contenir au moins un article.", "error");
        return;
      }
      button.closest("[data-line]").remove();
      updateTotals(page);
    });
    form.addEventListener("input", (event) => {
      const row = event.target.closest("[data-line]");
      if (!row) return;
      if (event.target.matches("[data-line-price-ttc]")) {
        row.querySelector("[data-line-price]").value = unitHtFromTtc(
          event.target.value,
          row.querySelector("[data-line-vat]").value,
        ).toFixed(2);
      }
      updateTotals(page, event.target);
    });
    form.addEventListener("change", (event) => {
      const row = event.target.closest("[data-line]");
      if (!row) return;
      if (event.target.matches("[data-line-family]")) {
        row.dataset.productFamily = event.target.value;
        return;
      }
      if (!event.target.matches("[data-line-product]")) return;
      const product = Store.byId.product(event.target.value);
      if (product) {
        syncProductSnapshot(row, product);
        row.querySelector("[data-line-price]").value = priceFor(product, isPurchase).toFixed(2);
        row.querySelector("[data-line-vat]").value = product.vat;
      }
      updateTotals(page, row.querySelector("[data-line-price]"));
    });
    form.addEventListener("submit", (event) => save(event, { existing, isPurchase, section, listRoute, autoGenerateReference, extraction, catalogueMode }));
    updateTotals(page);
    if (embedded) {
      page.classList.add("document-form-embedded");
      page.querySelector(".back-link")?.remove();
      page.querySelector(".form-page-head")?.remove();
      page.querySelectorAll("[data-submit-label]").forEach((node) => { node.textContent = "Confirmer et ajouter"; });
      return page;
    }
    Design.Shell.mount(page, collection, existing ? "Modifier" : "Ajouter", section.title);
    return page;
  }

  function configureDeliveryFastFlow(form, isDeliveryNote) {
    const noteSection = form.querySelector('textarea[name="notes"]')?.closest(".form-section");
    noteSection?.classList.toggle("document-note-section", Boolean(isDeliveryNote));
    if (!isDeliveryNote) return;

    const formLayout = form.querySelector(".form-layout");
    if (!formLayout || form.querySelector(".delivery-mobile-submit")) return;
    const action = document.createElement("div");
    action.className = "delivery-mobile-submit";
    action.innerHTML = '<button class="btn btn-primary" type="submit"><span class="material-symbols-rounded">check</span><span data-submit-label></span></button>';
    formLayout.insertAdjacentElement("afterend", action);
  }

  function formatNominationDatePart(format, date) {
    const month = String(date.getMonth() + 1).padStart(2, "0");
    if (format === "none") return "";
    if (format === "yyMM") return `${String(date.getFullYear()).slice(-2)}${month}`;
    if (format === "yyyyMM") return `${date.getFullYear()}${month}`;
    return month;
  }

  function suggestReference(setting, documentsOrNested, typeValue, date = new Date()) {
    const root = String(setting?.root || "").trim();
    if (!root) return null;
    const datePart = formatNominationDatePart(setting?.dateFormat || "MM", date);
    const size = Math.min(8, Math.max(1, Number(setting?.incrementSize) || 3));
    const prefix = [root, datePart].filter(Boolean).join("-");
    // documentsOrNested can be a single array (backwards compat) or an array of arrays (cross-nature scan).
    const documentSets = Array.isArray(documentsOrNested)
      ? (documentsOrNested.length > 0 && Array.isArray(documentsOrNested[0]) ? documentsOrNested : [documentsOrNested])
      : [];
    const maximum = maxSequenceInAllDocuments(documentSets, typeValue, prefix);
    return `${prefix}-${String(maximum + 1).padStart(size, "0")}`;
  }

  function maxSequenceInAllDocuments(documentsByNature, typeValue, prefix) {
    const expression = new RegExp(`^${prefix.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}-([0-9]+)$`);
    let maximum = 0;
    for (const documents of documentsByNature) {
      if (!Array.isArray(documents)) continue;
      for (const document of documents) {
        if (document?.typeValue !== typeValue) continue;
        const match = expression.exec(String(document?.ref || "").trim());
        if (match) maximum = Math.max(maximum, Number(match[1]));
      }
    }
    return maximum;
  }

  async function prefillSuggestedReference(form, { isPurchase, section }) {
    const reference = form.elements.reference;
    let edited = false;
    let setting = null;
    const updateSuggestion = () => {
      const date = new Date(`${form.elements.date.value || U.todayAndDue().today}T00:00:00`);
      // Scan both purchases AND sales to avoid suggesting a reference that already
      // exists on the other side when both share the same root prefix.
      const documentsByNature = [Store.state.purchases, Store.state.sales];
      const suggested = suggestReference(setting, documentsByNature, section.typeValue, date);
      if (!edited && suggested) reference.value = suggested;
    };
    reference.addEventListener("input", () => { edited = true; }, { once: true });
    form.elements.date.addEventListener("change", updateSuggestion);
    try {
      const key = `${isPurchase ? "achat" : "vente"}:${section.key}`;
      const nominations = await Design.Api.documents.nominations.list();
      setting = (Array.isArray(nominations) ? nominations : []).find((item) => String(item?.key ?? item?.Key) === key);
      updateSuggestion();
    } catch {
      // A blank editable reference deliberately falls back to server allocation.
    }
  }

  function render(kind, key, id) {
    return build(kind, key, id);
  }

  function showAllLineFields(page, isPurchase) {
    page.querySelectorAll("[data-line-field]").forEach((element) => {
      element.hidden = isPurchase && element.dataset.lineField === "margin";
    });
  }

  function renderMissingPrerequisite(isPurchase, section) {
    const missingProduct = !Store.state.products.length;
    const state = Design.Components.apiState({
      icon: missingProduct ? "bathroom" : isPurchase ? "local_shipping" : "person",
      eyebrow: "Pr\u{e9}requis manquant",
      title: missingProduct ? "Ajoutez d\u{2019}abord un produit" : `Ajoutez d\u{2019}abord un ${isPurchase ? "fournisseur" : "client"}`,
      description: "Un document doit \u{ea}tre li\u{e9} \u{e0} un article et \u{e0} un partenaire enregistr\u{e9}s.",
    });
    const button = Design.DocumentDom.missingAction();
    button.dataset.route = missingProduct ? "product-new" : isPurchase ? "supplier-new" : "client-new";
    state.appendChild(button);
    Design.Shell.mount(state, isPurchase ? "purchases" : "sales", "", section.title);
  }

  function lineRow(line, isPurchase, { catalogueMode = true } = {}) {
    const row = Design.DocumentDom.line();
    row.dataset.isPurchase = String(isPurchase);
    const product = catalogueMode ? Store.byId.product(line.productId) : null;
    row.dataset.lineId = line.id || "";
    const select = row.querySelector("[data-line-product]");
    const options = [];
    if (!catalogueMode) {
      options.push(new Option(line.productName || line.product || "Article extrait", "", true, true));
      select.disabled = true;
      select.required = false;
    } else {
      if (!product) {
        const extractedLabel = line.productName || line.product;
        options.push(new Option(extractedLabel ? "Extrait IA \u00b7 " + extractedLabel : "S\u00e9lectionner un produit\u2026", "", true, true));
      }
      options.push(...Store.state.products.map((item) => new Option(
        item.name || item.reference || "Article",
        item.id,
        false,
        item.id === product?.id,
      )));
      select.disabled = false;
    }
    select.replaceChildren(...options);
    if (product) select.value = product.id;
    syncProductSnapshot(row, product, line, { catalogueMode });
    row.querySelector("[data-line-family]")?.toggleAttribute("disabled", !catalogueMode);
    Design.Dom.setText(row, "[data-line-price-label]", isPurchase ? "Prix achat HT" : "Prix vente HT");
    row.querySelector("[data-line-quantity]").value = line.qty ?? 1;
    row.querySelector("[data-line-price]").value = (line.unit ?? (product ? priceFor(product, isPurchase) : 0)).toFixed(2);
    const lineVat = line.vat ?? product?.vat ?? 0;
    row.querySelector("[data-line-vat]").value = lineVat;
    const unitPrice = U.number(row.querySelector("[data-line-price]").value);
    const unitTtc = isPurchase
      ? unitPrice * (1 + lineVat / 100)
      : (line.priceTtc ?? unitPrice * (1 + lineVat / 100));
    row.querySelector("[data-line-price-ttc]").value = Number(unitTtc).toFixed(2);
    return row;
  }

  function fillFamilyOptions(row, selectedValue, catalogueMode = true) {
    const select = row.querySelector("[data-line-family]");
    if (!select) return;
    const selected = String(selectedValue || "").trim();
    const names = [...new Set([
      ...(catalogueMode ? (Store.state.families || []).map((item) => String(item.name || "").trim()) : []),
      selected,
    ].filter(Boolean))];
    select.replaceChildren(
      new Option("\u0053\u00e9lectionner une famille\u2026", "", !selected, !selected),
      ...names.map((name) => new Option(name, name, false, name === selected)),
    );
    select.value = selected;
  }

  function syncProductSnapshot(row, product, fallback = {}, { catalogueMode = true } = {}) {
    row.dataset.productName = product?.name || fallback.productName || fallback.product || "";
    row.dataset.productReference = product?.reference || fallback.productReference || fallback.ref || "";
    row.dataset.productFamily = fallback.productFamily || fallback.family || product?.family || "";
    row.dataset.productUnit = product?.unit || fallback.productUnit || "";
    row.dataset.productPurchase = String(product?.purchase ?? fallback.purchase ?? 0);
    row.dataset.productSale = String(product?.sale ?? fallback.sale ?? 0);
    Design.Dom.setText(row, "[data-line-reference]", row.dataset.productReference || "\u{2014}");
    fillFamilyOptions(row, row.dataset.productFamily, catalogueMode);
    Design.Dom.setText(row, "[data-line-unit]", row.dataset.productUnit || "\u{2014}");
  }

  function priceFor(product, isPurchase) {
    if (!product) return 0;
    return isPurchase ? product.purchase : product.sale / (1 + product.vat / 100 || 1);
  }

  function unitHtFromTtc(priceTtc, vat) {
    const multiplier = 1 + (Number(vat) || 0) / 100;
    return Number(((Number(priceTtc) || 0) / multiplier).toFixed(2));
  }

  function updateTotals(page, source = null) {
    let subtotal = 0;
    let tax = 0;
    page.querySelectorAll("[data-line]").forEach((row) => {
      const quantity = U.number(row.querySelector("[data-line-quantity]").value);
      const price = U.number(row.querySelector("[data-line-price]").value);
      const vat = U.number(row.querySelector("[data-line-vat]").value);
      const priceTtcInput = row.querySelector("[data-line-price-ttc]");
      const isPurchase = row.dataset.isPurchase === "true";
      let lineTotal;
      let lineTax;
      if (isPurchase) {
        const unitTtc = price * (1 + vat / 100);
        lineTotal = Number((quantity * price).toFixed(2));
        lineTax = Number((lineTotal * vat / 100).toFixed(2));
        if (priceTtcInput !== source) priceTtcInput.value = Number(unitTtc.toFixed(2));
      } else {
        const divisor = 1 + vat / 100;
        if (source === row.querySelector("[data-line-price]")) {
          priceTtcInput.value = Number((price * divisor).toFixed(2));
        }
        const unitTtc = U.number(priceTtcInput.value);
        const lineTotalTtc = Number((quantity * unitTtc).toFixed(2));
        lineTotal = Number((lineTotalTtc / divisor).toFixed(2));
        lineTax = Number((lineTotalTtc - lineTotal).toFixed(2));
        if (source !== row.querySelector("[data-line-price]")) {
          row.querySelector("[data-line-price]").value = Number((unitTtc / divisor).toFixed(2));
        }
      }
      const costHt = isPurchase ? price : U.number(row.dataset.productPurchase);
      const saleHt = isPurchase
        ? U.number(row.dataset.productSale) / (1 + vat / 100 || 1)
        : price;
      const margin = costHt > 0 ? ((saleHt - costHt) / costHt) * 100 : 0;
      subtotal += lineTotal;
      tax += lineTax;
      Design.Dom.setText(row, "[data-line-margin]", costHt > 0 ? `${margin.toFixed(1)} %` : "\u{2014}");
      Design.Dom.setText(row, "[data-line-total]", U.money(lineTotal));
      Design.Dom.setText(row, "[data-line-total-ttc]", U.money(lineTotal + lineTax));
    });
    Design.Dom.setText(page, "[data-subtotal]", U.money(subtotal));
    Design.Dom.setText(page, "[data-tax]", U.money(tax));
    Design.Dom.setText(page, "[data-total]", U.money(subtotal + tax));
  }

  async function save(event, context) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!Design.DocumentValidators.validate(form)) return;
    U.setSubmitting(form, true);
    try {
      const payload = Design.DocumentMappers.toPayload(form, { isPurchase: context.isPurchase, typeValue: context.section.typeValue, autoGenerateReference: context.autoGenerateReference, extraction: context.extraction, catalogueMode: context.catalogueMode });
      if (context.existing && Number(payload.status) === 3 && Number(context.existing.totalPaid || 0) > 0) {
        Design.Components.toast(
          "Annulation impossible",
          "Ce document contient d\u00e9j\u00e0 un r\u00e8glement. Annulez d\u2019abord le r\u00e8glement avant d\u2019annuler le document.",
          "error",
        );
        return;
      }
      const saved = context.existing
        ? await Design.Api.documents.update(context.existing.id, payload)
        : await Design.Api.documents.create(payload);
      // If the API returned the full invoice DTO (update now returns InvoiceDto),
      // inject it into the store immediately so the detail view renders fresh data
      // without waiting for a full reload from the (possibly cached) GET endpoint.
      const mapped = saved?.invoiceId || saved?.InvoiceId
        ? Design.DocumentMappers.mapDocument(saved)
        : null;
      if (mapped?.id) {
        const collection = context.isPurchase ? Store.state.purchases : Store.state.sales;
        const index = collection.findIndex((item) => item.id === mapped.id);
        if (index >= 0) collection[index] = mapped;
        else collection.unshift(mapped);
      }
      const id = String(mapped?.id || saved?.invoiceId || saved?.InvoiceId || context.existing?.id || "");
      await Design.WorkspacePage.finalizeMutation({
        successTitle: context.existing ? "Document mis \u{e0} jour" : "Document cr\u{e9}\u{e9}",
        successMessage: "Les donn\u{e9}es ont \u{e9}t\u{e9} enregistr\u{e9}es dans l\u{2019}API.",
        onRefreshed: () => {
          const exists = context.isPurchase ? Store.byId.purchase(id) : Store.byId.sale(id);
          Design.Router.go(exists ? `${context.isPurchase ? "purchase" : "sale"}/${id}` : context.listRoute);
        },
        onRefreshFailed: () => Design.Router.go(context.listRoute),
      });
    } catch (error) {
      Design.Components.toast("Enregistrement impossible", error.message, "error");
    } finally {
      U.setSubmitting(form, false);
    }
  }

  Design.DocumentForms = { build, render, suggestReference };
})();
