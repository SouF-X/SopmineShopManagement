(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const U = Design.Utils;
  const C = Design.Components;
  const Data = Design.DocumentData;
  const LOCKED_PREVIEW_FIELDS = new Set(["product", "quantity"]);
  function showList(kind, key) {
    const isPurchase = kind === "purchases";
    const documents = isPurchase ? Store.state.purchases : Store.state.sales;
    const section = Data.section(kind, key);
    const page = Design.DocumentDom.page();
    page.dataset.documentKind = kind;
    page.dataset.documentSection = section.key;
    C.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: `${isPurchase ? "Achats" : "Ventes"} · espace dédié`, icon: section.icon,
      title: section.title, count: documents.filter((item) => item.type === section.type).length,
      description: section.description, actionLabel: section.action,
      actionRoute: `${isPurchase ? "purchase-new" : "sale-new"}/${section.key}`, secondaryLabel: "Exporter",
    });
    page.querySelector("[data-partner-heading]").textContent = isPurchase ? "Fournisseur" : "Client";
    page.querySelector("[data-document-navigation]").replaceChildren(...renderNavigation(kind, key, documents));
    const pendingPartnerId = Store.state.pendingDocumentPartnerId;
    if (pendingPartnerId) {
      const partner = isPurchase ? Store.byId.supplier(pendingPartnerId) : Store.byId.client(pendingPartnerId);
      page.querySelector("[data-document-search]").value = partner?.name || "";
      Store.state.pendingDocumentPartnerId = null;
    }
    page.dataset.currentPage = "1";
    const update = (resetPage = true) => {
      if (resetPage) page.dataset.currentPage = "1";
      updateRows(page, kind, section, documents);
    };
    page.querySelector("[data-document-search]").addEventListener("input", () => update());
    page.querySelector("[data-document-status]").addEventListener("change", () => update());
    page.querySelector("[data-document-period]").addEventListener("change", () => update());
    if (!isPurchase && section.key === "bonlivraison") mountDeliveryConversion(page, documents);
    Design.Shell.mount(page, kind, "", section.title);
    update();
  }

  function mountDeliveryConversion(page, documents) {
    const available = documents.filter((item) => item.typeValue === 3
      && item.natureValue === 1
      && [1, 2].includes(item.statusValue));
    const eligible = available.filter((item) => !item.convertedToInvoiceId);
    if (!available.length) return;
    const action = Design.DocumentDom.deliveryAction();
    page.querySelector(".toolbar-actions").prepend(action);
    action.addEventListener("click", () => {
      if (!eligible.length) {
        C.toast("Aucun BL disponible", "Tous les bons de livraison validés ont déjà été convertis.", "error");
        return;
      }
      openDeliveryConversion(eligible);
    });
  }

  function groupDeliveriesByClient(deliveries) {
    const groups = deliveries.reduce((result, delivery) => {
      const client = Store.byId.client(delivery.partnerId) || { name: "Client indisponible", city: "", ice: "" };
      const group = result.get(delivery.partnerId) || { clientId: delivery.partnerId, client, deliveries: [], total: 0 };
      group.deliveries.push(delivery);
      group.total += Number(delivery.amount || 0);
      result.set(delivery.partnerId, group);
      return result;
    }, new Map());
    return [...groups.values()].sort((left, right) => left.client.name.localeCompare(right.client.name, "fr", { sensitivity: "base" }));
  }

  function openDeliveryConversion(deliveries) {
    const dialog = Design.DocumentDom.deliveryDialog();
    const panel = dialog.querySelector("[data-delivery-form]");
    const picker = panel.querySelector("[data-delivery-client-picker]");
    const blPicker = panel.querySelector("[data-delivery-bl-picker]");
    const grid = panel.querySelector("[data-delivery-client-grid]");
    const list = panel.querySelector("[data-delivery-list]");
    const summary = panel.querySelector("[data-delivery-summary]");
    const confirm = panel.querySelector("[data-confirm]");
    const groups = groupDeliveriesByClient(deliveries);
    let selectedClientId = null;
    const selectedIds = new Set();

    const clientMeta = (client) => [client.city, client.ice ? `ICE ${client.ice}` : ""].filter(Boolean).join(" · ") || "Client";
    const syncSelection = () => {
      const selected = groups.find((group) => group.clientId === selectedClientId)?.deliveries.filter((delivery) => selectedIds.has(delivery.id)) || [];
      const total = selected.reduce((sum, delivery) => sum + Number(delivery.amount || 0), 0);
      Design.Dom.setText(panel, "[data-delivery-selection-count]", String(selected.length));
      Design.Dom.setText(panel, "[data-delivery-selection-total]", U.money(total));
      confirm.disabled = selected.length === 0;
      const selectAll = panel.querySelector("[data-delivery-select-all]");
      const rows = [...list.querySelectorAll("[data-delivery-checkbox]")];
      selectAll.checked = rows.length > 0 && selected.length === rows.length;
      selectAll.indeterminate = selected.length > 0 && selected.length < rows.length;
      rows.forEach((checkbox) => { checkbox.checked = selectedIds.has(checkbox.value); });
    };
    const selectClient = (clientId) => {
      selectedClientId = clientId;
      selectedIds.clear();
      const group = groups.find((item) => item.clientId === clientId);
      Design.Dom.setText(panel, "[data-delivery-selected-client-name]", group.client.name);
      Design.Dom.setText(panel, "[data-delivery-selected-client-meta]", clientMeta(group.client));
      Design.Dom.setText(panel, "[data-delivery-selection-client]", group.client.name);
      list.replaceChildren(...group.deliveries.map((delivery) => {
        const row = Design.DocumentDom.deliveryRow();
        const checkbox = row.querySelector("[data-delivery-checkbox]");
        checkbox.value = delivery.id;
        checkbox.setAttribute("aria-label", `Sélectionner ${delivery.ref} pour ${group.client.name}`);
        Design.Dom.setText(row, "[data-delivery-reference]", delivery.ref);
        Design.Dom.setText(row, "[data-delivery-date]", delivery.date);
        Design.Dom.setText(row, "[data-delivery-amount]", U.money(delivery.amount));
        return row;
      }));
      picker.hidden = true;
      blPicker.hidden = false;
      summary.hidden = false;
      Design.Dom.setText(panel, "[data-delivery-step-copy]", "Sélectionnez les bons de livraison à ajouter à cette facture.");
      syncSelection();
    };
    groups.forEach((group) => {
      const card = Design.DocumentDom.deliveryClientCard();
      card.dataset.clientId = group.clientId;
      Design.Dom.setText(card, "[data-delivery-client-initials]", U.initials(group.client.name));
      Design.Dom.setText(card, "[data-delivery-client-name]", group.client.name);
      Design.Dom.setText(card, "[data-delivery-client-meta]", clientMeta(group.client));
      Design.Dom.setText(card, "[data-delivery-client-count]", `${group.deliveries.length} BL à facturer`);
      Design.Dom.setText(card, "[data-delivery-client-total]", U.money(group.total));
      card.addEventListener("click", () => selectClient(group.clientId));
      grid.appendChild(card);
    });
    panel.querySelector("[data-delivery-search]").addEventListener("input", (event) => {
      const query = U.normalizeSearch(event.target.value);
      grid.querySelectorAll("[data-delivery-client-card]").forEach((card) => { card.hidden = Boolean(query) && !U.normalizeSearch(card.textContent).includes(query); });
    });
    list.addEventListener("change", (event) => {
      const checkbox = event.target.closest("[data-delivery-checkbox]");
      if (!checkbox) return;
      checkbox.checked ? selectedIds.add(checkbox.value) : selectedIds.delete(checkbox.value);
      syncSelection();
    });
    panel.querySelector("[data-delivery-select-all]").addEventListener("change", (event) => {
      list.querySelectorAll("[data-delivery-checkbox]").forEach((checkbox) => { if (event.target.checked) selectedIds.add(checkbox.value); else selectedIds.delete(checkbox.value); });
      syncSelection();
    });
    panel.querySelector("[data-delivery-change-client]").addEventListener("click", () => {
      selectedClientId = null;
      selectedIds.clear();
      picker.hidden = false;
      blPicker.hidden = true;
      summary.hidden = true;
      Design.Dom.setText(panel, "[data-delivery-step-copy]", "Choisissez d’abord le client à facturer.");
    });
    panel.querySelectorAll("[data-cancel]").forEach((button) => button.addEventListener("click", () => dialog.close()));
    panel.addEventListener("submit", async (event) => {
      event.preventDefault();
      const invoiceIds = [...selectedIds];
      if (!invoiceIds.length) return;
      confirm.disabled = true;
      try {
        const created = await Design.Api.documents.convertDeliveryNotes(invoiceIds);
        await Design.WorkspacePage.reload();
        dialog.close();
        Design.Router.go(`sale/${created.invoiceId || created.InvoiceId}`);
        C.toast("Facture créée", `${invoiceIds.length} bon${invoiceIds.length > 1 ? "s" : ""} de livraison converti${invoiceIds.length > 1 ? "s" : ""}.`);
      } catch (error) {
        syncSelection();
        C.toast("Conversion impossible", error.message, "error");
      }
    });
    document.body.appendChild(dialog);
    dialog.addEventListener("close", () => dialog.remove());
    dialog.showModal();
  }

  function renderNavigation(kind, activeKey, documents) {
    const items = Data.sections[kind].map((section) => navItem(kind, section, activeKey, documents));
    if (kind === "purchases") items.push(aiNavItem(activeKey));
    return items;
  }

  function navItem(kind, section, activeKey, documents) {
    const item = Design.DocumentDom.nav();
    item.dataset.route = `${kind}/${section.key}`;
    item.classList.toggle("is-active", section.key === activeKey);
    Design.Dom.setText(item, "[data-nav-icon]", section.icon);
    Design.Dom.setText(item, "[data-nav-title]", section.title);
    const count = documents.filter((documentItem) => documentItem.type === section.type).length;
    Design.Dom.setText(item, "[data-nav-count]", `${count} document${count > 1 ? "s" : ""}`);
    return item;
  }

  function aiNavItem(activeKey) {
    const item = Design.DocumentDom.nav();
    const workspace = Data.aiInvoiceWorkspace;
    item.classList.add("document-ai-nav");
    item.dataset.route = `purchases/${workspace.key}`;
    item.classList.toggle("is-active", workspace.key === activeKey);
    Design.Dom.setText(item, "[data-nav-icon]", workspace.icon);
    Design.Dom.setText(item, "[data-nav-title]", workspace.title);
    Design.Dom.setText(item, "[data-nav-count]", workspace.action);
    return item;
  }

  function updateRows(page, kind, section, documents) {
    const isPurchase = kind === "purchases";
    const query = U.normalizeSearch(page.querySelector("[data-document-search]").value);
    const status = page.querySelector("[data-document-status]").value;
    const period = page.querySelector("[data-document-period]").value;
    const now = new Date();
    const filtered = documents.filter((documentItem) => {
      if (documentItem.type !== section.type) return false;
      const partner = isPurchase ? Store.byId.supplier(documentItem.partnerId) : Store.byId.client(documentItem.partnerId);
      const searchable = U.normalizeSearch(`${documentItem.ref} ${partner?.name || ""} ${documentItem.status}`);
      const date = new Date(`${documentItem.dateValue}T00:00:00`);
      const age = (now - date) / 86400000;
      const periodMatch = !period || (period === "week" && age >= 0 && age <= 7) || (period === "month" && date.getMonth() === now.getMonth() && date.getFullYear() === now.getFullYear());
      return searchable.includes(query) && (!status || documentItem.status === status) && periodMatch;
    });
    const pageSize = 10;
    const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
    const currentPage = Math.min(Number(page.dataset.currentPage || 1), pageCount);
    page.dataset.currentPage = String(currentPage);
    const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
    const body = page.querySelector("[data-document-list]");
    if (visible.length) body.replaceChildren(...visible.map((item) => fillRow(item, isPurchase)));
    else {
      const row = Design.DocumentDom.emptyRow();
      row.querySelector("[data-document-empty-cell]").appendChild(C.emptyState("description", `Aucun ${section.singular}`, "Ajoutez un document pour commencer."));
      body.replaceChildren(row);
    }
    Design.Dom.setText(page, "[data-document-total]", `Total · ${U.money(filtered.reduce((sum, item) => sum + item.amount, 0))}`);
    page.querySelector("[data-document-footer]").replaceChildren(C.collectionFooter(filtered.length, section.title.toLowerCase(), {
      page: currentPage,
      pageSize,
      onPage: (nextPage) => {
        page.dataset.currentPage = String(nextPage);
        updateRows(page, kind, section, documents);
      },
    }));
  }

  function fillRow(documentItem, isPurchase) {
    const row = Design.DocumentDom.row();
    const partner = (isPurchase ? Store.byId.supplier(documentItem.partnerId) : Store.byId.client(documentItem.partnerId)) || { name: "Partenaire indisponible", city: "—" };
    const presentationStatus = statusForPresentation(documentItem);
    const route = `${isPurchase ? "purchase" : "sale"}/${documentItem.id}`;
    row.dataset.open = route;
    row.querySelector("[data-route]").dataset.route = route;
    Design.Dom.setText(row, "[data-document-reference]", documentItem.ref);
    Design.Dom.setText(row, "[data-document-type]", `${documentItem.lines.length} article${documentItem.lines.length > 1 ? "s" : ""}`);
    Design.Dom.setText(row, "[data-partner-initials]", U.initials(partner.name));
    row.querySelector("[data-partner-initials]").classList.add(isPurchase ? "supplier" : "client");
    Design.Dom.setText(row, "[data-partner-name]", partner.name);
    Design.Dom.setText(row, "[data-partner-city]", partner.city);
    Design.Dom.setText(row, "[data-document-date]", documentItem.date);
    Design.Dom.setText(row, "[data-document-due]", documentItem.dueValue ? `Échéance ${documentItem.due}` : "");
    row.querySelector("[data-document-status]").replaceWith(C.status(presentationStatus.label));
    const payment = row.querySelector("[data-payment-status]");
    const progressLabel = Design.DocumentPayments?.progressLabel(documentItem);
    const showPayment = Boolean(progressLabel);
    payment.hidden = !showPayment;
    payment.textContent = progressLabel || "";
    Design.Dom.setText(row, "[data-document-total]", U.money(documentItem.amount));
    return row;
  }

  function showDetail(kind, id) {
    const isPurchase = kind === "purchase";
    const documentItem = isPurchase ? Store.byId.purchase(id) : Store.byId.sale(id);
    if (!documentItem) return Design.Shell.missing("Ce document n’existe plus", isPurchase ? "purchases/boncommande" : "sales/devis");
    const collection = isPurchase ? "purchases" : "sales";
    const section = Data.sections[collection].find((item) => item.type === documentItem.type) || Data.sections[collection][0];
    const partner = (isPurchase ? Store.byId.supplier(documentItem.partnerId) : Store.byId.client(documentItem.partnerId)) || { name: "Partenaire indisponible", address: "—", ice: "—" };
    const page = Design.DocumentDom.detail();
    const presentationStatus = statusForPresentation(documentItem);
    page.querySelector("[data-back]").dataset.route = `${collection}/${section.key}`;
    fillAll(page, "[data-document-type]", documentItem.type);
    fillAll(page, "[data-document-reference]", documentItem.ref);
    fillAll(page, "[data-document-date]", documentItem.date);
    fillAll(page, "[data-document-due]", documentItem.due);
    fillAll(page, "[data-partner-name]", partner.name);
    Design.Dom.setText(page, "[data-partner-label]", isPurchase ? "Fournisseur" : "Client");
    Design.Dom.setText(page, "[data-partner-address]", `${partner.address || "—"} · ICE ${partner.ice || "—"}`);
    page.querySelector("[data-document-status]").replaceWith(C.status(presentationStatus.label));
    page.querySelector("[data-side-status]").replaceWith(C.status(presentationStatus.label));
    const isPayableDocument = Design.DocumentPayments?.isPayableDocument;
    const progressLabel = Design.DocumentPayments?.progressLabel(documentItem)
      || (isPayableDocument?.(documentItem) ? documentItem.paymentStatus : null)
      || "—";
    fillAll(page, "[data-payment-status]", progressLabel);
    fillAll(page, "[data-payment-method]", documentItem.paymentMethod || "—");
    fillAll(page, "[data-subtotal]", U.money(documentItem.subtotal));
    fillAll(page, "[data-tax]", U.money(documentItem.taxTotal));
    fillAll(page, "[data-total]", U.money(documentItem.amount));
    const presentationLines = Design.DocumentMappers.linesForPresentation(documentItem.lines);
    page.querySelector("[data-paper-lines]").replaceChildren(...presentationLines.map(paperLine));
    fillStatusTrack(page.querySelector("[data-status-track]"), documentItem);
    const recordPayment = page.querySelector("[data-record-payment]");
    const remainingAmount = Math.max(0, Number(documentItem.amount || 0) - Number(documentItem.totalPaid || 0));
    const canRecordPayment = isPayableDocument?.(documentItem) && remainingAmount > 0;
    recordPayment.hidden = !canRecordPayment;
    if (canRecordPayment) {
      recordPayment.addEventListener("click", () => Design.DocumentPayments?.open(documentItem));
    }

    const settlementPanel = page.querySelector("[data-payments-panel]");
    settlementPanel.hidden = true;
    settlementPanel.replaceChildren();
    const isPaidDocumentLocked = Design.DocumentPayments?.isPaymentLockedDocument?.(documentItem) === true;
    const isConvertedDocumentLocked = Boolean(documentItem.convertedToInvoiceId);
    const isCancelledDocumentLocked = Number(documentItem.statusValue) === 3;
    const isDocumentLocked = isPaidDocumentLocked || isConvertedDocumentLocked || isCancelledDocumentLocked;
    const editButton = page.querySelector("[data-edit]");
    const deleteButton = page.querySelector("[data-delete]");
    if (isDocumentLocked) {
      const lockMessage = isConvertedDocumentLocked
        ? "Bon de livraison facturé — modifications et règlements disponibles sur la facture."
        : isCancelledDocumentLocked
          ? "Document annulé — modification et suppression verrouillées."
          : "Document réglé — modification et suppression verrouillées.";
      [editButton, deleteButton].forEach((button) => {
        button.disabled = true;
        button.title = lockMessage;
        button.querySelector(".material-symbols-rounded").textContent = "lock";
      });
    } else {
      editButton.dataset.route = `${isPurchase ? "purchase-edit" : "sale-edit"}/${id}`;
      deleteButton.addEventListener("click", () => Design.DocumentPage.remove(documentItem, isPurchase));
    }
    const printOptions = Design.DocumentPrint.resolveOptions(kind, documentItem.typeValue);
    const printButton = page.querySelector("[data-print]");
    const twoCopiesControl = page.querySelector("[data-print-two-copies]");
    const signatureControl = page.querySelector("[data-print-signature]");
    const totalsModeControl = page.querySelector("[data-print-totals-mode-control]");
    const twoCopiesToggle = page.querySelector("[data-print-two-copies-toggle]");
    const signatureToggle = page.querySelector("[data-print-signature-toggle]");
    const totalsModeSelect = page.querySelector("[data-print-totals-mode]");
    totalsModeSelect.querySelector('option[value="total"]').textContent = isPurchase ? "Total TTC" : "Total";
    printButton.hidden = !printOptions.pdf;
    twoCopiesControl.hidden = !printOptions.twoCopies;
    signatureControl.hidden = !printOptions.signature;
    totalsModeControl.hidden = !printOptions.pdf;
    let twoCopies = false;
    let showSignature = false;
    let totalsMode = Design.DocumentPrint.getTotalsMode();
    totalsModeSelect.value = totalsMode;
    totalsModeSelect.dispatchEvent(new Event("change", { bubbles: true }));
    twoCopiesToggle.addEventListener("change", () => { twoCopies = twoCopiesToggle.checked; });
    signatureToggle.addEventListener("change", () => {
      showSignature = signatureToggle.checked;
      renderPreview();
    });
    const paperShell = page.querySelector(".document-paper-shell");
    const renderPreview = () => Design.DocumentPrint.open(documentItem, partner, {
      twoCopies: false,
      showSignature,
      totalsMode,
      previewTarget: paperShell,
    });
    totalsModeSelect.addEventListener("change", () => {
      totalsMode = Design.DocumentPrint.setTotalsMode(totalsModeSelect.value);
      renderPreview();
    });
    const previewFieldRows = page.querySelectorAll("[data-preview-field]");
    const purchaseHiddenFields = new Set(["priceHt", "margin", "totalTtc"]);
    previewFieldRows.forEach((row) => {
      row.toggleAttribute("hidden", isPurchase && purchaseHiddenFields.has(row.dataset.previewField));
    });
    let visibleLineFields = Design.DocumentPrint.visibleLineFields(isPurchase ? "purchase" : "sale");
    const applyFields = () => {
      previewFieldRows.forEach((row) => {
        row.querySelector('input[type="checkbox"]').checked = visibleLineFields.has(row.dataset.previewField);
      });
    };
    applyFields();
    previewFieldRows.forEach((row) => row.addEventListener("change", () => {
      const field = row.dataset.previewField;
      if (LOCKED_PREVIEW_FIELDS.has(field)) return;
      if (row.querySelector('input[type="checkbox"]').checked) visibleLineFields.add(field);
      else visibleLineFields.delete(field);
      Design.DocumentPrint.saveVisibleLineFields(isPurchase ? "purchase" : "sale", visibleLineFields);
      applyFields();
      renderPreview();
    }));
    printButton.addEventListener("click", () => Design.DocumentPrint.open(documentItem, partner, { twoCopies, showSignature, totalsMode }));
    Design.Shell.mount(page, collection, "Détails", section.title);
    renderPreview();
  }


  function paperLine(line) {
    const row = Design.DocumentDom.paperLine();
    Design.Dom.setText(row, "[data-line-name]", line.product);
    Design.Dom.setText(row, "[data-line-reference]", line.ref);
    Design.Dom.setText(row, "[data-line-quantity]", line.qty);
    Design.Dom.setText(row, "[data-line-price]", U.money(line.unit));
    Design.Dom.setText(row, "[data-line-total]", U.money(line.qty * line.unit));
    return row;
  }

  function fillStatusTrack(host, documentItem) {
    const presentationStatus = statusForPresentation(documentItem);
    const hasPaymentStep = ![0, 1, 2, 5].includes(Number(documentItem.typeValue));
    const steps = presentationStatus.label === "Annulé"
      ? [["block", "Annulé", "Ce document n’est plus actif", true]]
      : hasPaymentStep
        ? [["edit_note", "Brouillon", "Document en préparation", presentationStatus.value === 0], ["verified", "Validé", "Document confirmé", presentationStatus.value === 1], ["payments", "Payé", "Règlement enregistré", presentationStatus.value === 2]]
        : [["edit_note", "Brouillon", "Document en préparation", presentationStatus.value === 0], ["verified", "Validé", "Document confirmé", presentationStatus.value === 1]];
    host.replaceChildren(...steps.map((step, index) => {
      const item = Design.DocumentDom.flowStep();
      const isComplete = presentationStatus.label !== "Annulé" && (index < presentationStatus.value || presentationStatus.value === 2);
      if (isComplete) item.classList.add("is-done");
      if (isComplete && step[3]) item.classList.add("is-current");
      else if (step[3]) item.classList.add("is-current");
      Design.Dom.setText(item, "[data-step-icon]", isComplete ? "check" : step[0]);
      Design.Dom.setText(item, "[data-step-title]", step[1]);
      Design.Dom.setText(item, "[data-step-text]", isComplete ? "Terminé" : step[2]);
      return item;
    }));
  }

  function statusForPresentation(documentItem) {
    const linkedInvoice = documentItem.convertedToInvoiceId
      ? Store.byId.sale(String(documentItem.convertedToInvoiceId))
      : null;
    if (linkedInvoice?.typeValue === 4 && linkedInvoice.statusValue === 2) {
      return { value: 2, label: "Payé" };
    }
    return { value: documentItem.statusValue, label: documentItem.status };
  }

  function fillAll(root, selector, value) {
    root.querySelectorAll(selector).forEach((node) => { node.textContent = value; });
  }

  Design.DocumentList = { showList, showDetail, renderNavigation };
})();
