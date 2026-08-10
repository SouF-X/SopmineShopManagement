(function () {
  const Design = window.SopmineDesign;
  const U = Design.Utils;
  const C = Design.Components;

  const METHODS = [
    [0, "Espèces"],
    [1, "Chèque"],
    [2, "Virement"],
    [3, "Effet"],
    [4, "Carte"],
  ];
  const PROGRESS = {
    unpaid: ["Non réglée", "pending_actions"],
    partial: ["Partiellement réglée", "hourglass_top"],
    paid: ["Réglée", "task_alt"],
    overdue: ["En retard", "warning"],
  };

  function money(value) {
    return U.money(Number(value || 0));
  }

  function today() {
    return new Date().toISOString().slice(0, 10);
  }

  function isPayableDocument(documentItem) {
    if (documentItem?.convertedToInvoiceId) return false;
    const nature = Number(documentItem?.natureValue);
    const type = Number(documentItem?.typeValue);
    const status = Number(documentItem?.statusValue);
    const isPurchaseInvoice = nature === 0 && type === 4;
    const isSalesDeliveryOrInvoice = nature === 1 && (type === 3 || type === 4);
    return (isPurchaseInvoice || isSalesDeliveryOrInvoice)
      && (status === 1 || status === 2);
  }
  function isPaymentLockedDocument(documentItem) {
    return isPayableDocument(documentItem)
      && documentItem?.paymentProgress === "paid";
  }

  function paymentFlowCopy(documentItem) {
    const isPurchase = Number(documentItem?.natureValue) === 0;
    return isPurchase
      ? { eyebrow: "D\u00e9caissement fournisseur", completed: "D\u00e9caiss\u00e9", success: "D\u00e9caissement enregistr\u00e9" }
      : { eyebrow: "Encaissement client", completed: "Encaiss\u00e9", success: "Encaissement enregistr\u00e9" };
  }

  function progressOf(documentItem) {
    return PROGRESS[documentItem?.paymentProgress] || PROGRESS.unpaid;
  }

  function progressLabel(documentItem) {
    if (documentItem?.convertedToInvoiceId) return "Factur\u00e9";
    if (!isPayableDocument(documentItem)) return null;
    return progressOf(documentItem)[0];
  }

  function ensurePanel(root) {
    if (root) return root;
    const page = document.getElementById("document-detail-view");
    const side = page?.querySelector(".document-side");
    if (!side) return null;
    const panel = document.createElement("section");
    panel.className = "record-panel payments-panel";
    panel.dataset.paymentsPanel = "";
    side.appendChild(panel);
    return panel;
  }

  function mount(root) {
    const panel = ensurePanel(root);
    if (!panel) return;
    panel.hidden = true;
    panel.replaceChildren();
  }

  function paymentSummary(documentItem, payments) {
    const total = Number(documentItem.amount || 0);
    const paid = payments
      .filter((payment) => !payment.cancelledAtUtc && !payment.isOpeningBalance)
      .reduce((sum, payment) => sum + payment.amount, 0);
    const remaining = Math.max(0, total - paid);
    return { total, paid, remaining, ratio: total > 0 ? Math.min(100, Math.max(0, (paid / total) * 100)) : 0 };
  }

  function progressKey(summary) {
    return summary.remaining <= 0 ? "paid" : summary.paid > 0 ? "partial" : "unpaid";
  }

  function sortPayments(payments, newestFirst = false) {
    const direction = newestFirst ? -1 : 1;
    return [...payments].sort((left, right) => {
      const leftDate = Date.parse(left.paymentDate || left.cancelledAtUtc || 0) || 0;
      const rightDate = Date.parse(right.paymentDate || right.cancelledAtUtc || 0) || 0;
      return (leftDate - rightDate) * direction || left.id.localeCompare(right.id) * direction;
    });
  }

  function renderRows(payments, documentItem, options) {
    if (!payments.length) return [C.emptyState("payments", "Aucun règlement", "Ajoutez le premier encaissement depuis ce document.")];
    const timeline = document.createElement("div");
    timeline.className = "payment-timeline";
    timeline.setAttribute("role", "list");
    timeline.replaceChildren(...payments.map((payment) => paymentRow(payment, documentItem, options)));
    return [timeline];
  }

  function mapPayment(dto) {
    return {
      id: String(dto.invoicePaymentId || dto.InvoicePaymentId || dto.paymentId || dto.PaymentId || dto.id || dto.Id || ""),
      amount: Number(dto.amount ?? dto.Amount ?? 0),
      paymentDate: dto.paymentDate || dto.PaymentDate || null,
      method: methodLabel(dto.method ?? dto.Method),
      reference: dto.reference || dto.Reference || "",
      note: dto.note || dto.Note || "",
      cancelledAtUtc: dto.cancelledAtUtc || dto.CancelledAtUtc || null,
      isOpeningBalance: Boolean(dto.isOpeningBalance ?? dto.IsOpeningBalance),
    };
  }

  function methodLabel(value) {
    if (value == null || value === "") return "Ouverture";
    if (typeof value === "number") return METHODS.find(([key]) => key === value)?.[1] || "Règlement";
    const text = String(value);
    const numeric = Number(text);
    if (Number.isFinite(numeric) && text.trim() !== "") return METHODS.find(([key]) => key === numeric)?.[1] || "Règlement";
    const normalized = text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
    const byName = { espece: "Espèces", cheque: "Chèque", virement: "Virement", effet: "Effet", carte: "Carte" };
    return byName[normalized] || text;
  }

  function paymentRow(payment, documentItem, options) {
    const row = document.createElement("article");
    const cancelled = Boolean(payment.cancelledAtUtc);
    const state = cancelled ? "Annul\u00e9" : payment.isOpeningBalance ? "Ouverture" : paymentFlowCopy(documentItem).completed;
    const cancellationDate = cancelled ? ` · ${U.formatDate(payment.cancelledAtUtc)}` : "";
    row.className = "payment-row";
    row.classList.toggle("is-cancelled", cancelled);
    row.setAttribute("role", "listitem");
    row.innerHTML = `
      <span class="payment-row-icon material-symbols-rounded" aria-hidden="true">${cancelled ? "block" : "payments"}</span>
      <div class="payment-row-copy">
        <header><strong>${escapeHtml(payment.method)}</strong><time datetime="${escapeHtml(payment.paymentDate || "")}">${escapeHtml(U.formatDate(payment.paymentDate))}</time></header>
        <p class="payment-row-meta"><span>Référence</span><b>${payment.reference ? escapeHtml(payment.reference) : "—"}</b></p>
        ${payment.note ? `<p class="payment-row-note">${escapeHtml(payment.note)}</p>` : ""}
      </div>
      <div class="payment-row-amount"><strong>${money(payment.amount)}</strong><span>${state}${cancellationDate}</span></div>
      <button class="table-icon-btn" type="button" data-cancel-payment aria-label="Annuler le règlement"><span class="material-symbols-rounded">undo</span></button>`;
    const cancel = row.querySelector("[data-cancel-payment]");
    cancel.hidden = cancelled || payment.isOpeningBalance;
    cancel.addEventListener("click", () => cancelPayment(documentItem, payment, options));
    return row;
  }

  function open(invoice, options = {}) {
    if (!isPayableDocument(invoice)) return;
    let available = Math.max(0, Number(invoice.amount ?? 0) - Number(invoice.totalPaid ?? 0));
    if (available <= 0) return;
    const initialPaid = Number(invoice.totalPaid || 0);
    const initialRatio = Number(invoice.amount || 0) > 0 ? Math.min(100, Math.max(0, (initialPaid / Number(invoice.amount || 0)) * 100)) : 0;

    const dialog = document.createElement("dialog");
    dialog.className = "payment-dialog payment-drawer";
    dialog.setAttribute("aria-labelledby", "payment-drawer-title");
    dialog.innerHTML = `
      <form class="payment-dialog-panel" method="dialog" data-payment-form>
        <header><div class="payment-drawer-title"><span class="payment-drawer-icon material-symbols-rounded" aria-hidden="true">account_balance_wallet</span><div><span class="eyebrow">${paymentFlowCopy(invoice).eyebrow}</span><h2 id="payment-drawer-title">Règlements</h2><p>Suivez le solde et les mouvements de ce document.</p></div></div><button class="table-icon-btn" type="button" data-close aria-label="Fermer les règlements"><span class="material-symbols-rounded">close</span></button></header>
        <div class="payment-drawer-content">
          <section class="payment-drawer-balance" aria-label="Résumé des règlements">
            <section class="payment-settlement-hero">
              <div class="payment-drawer-balance-head">
                <div class="payment-drawer-balance-context">
                  <span class="payment-progress-badge" data-progress="${invoice.paymentProgress || "unpaid"}"><span class="material-symbols-rounded">${progressOf(invoice)[1]}</span>${progressOf(invoice)[0]}</span>
                  <span class="payment-drawer-reference">Réf. ${escapeHtml(invoice.ref || "—")}</span>
                  <button class="btn btn-secondary payment-history-open" type="button" data-payment-history-open><span class="material-symbols-rounded" aria-hidden="true">receipt_long</span><span>Historique des paiements</span><span class="payment-history-count" data-payment-history-count aria-label="Chargement du nombre de paiements">…</span></button>
                </div>
                <div class="payment-drawer-remaining"><span>Solde restant</span><strong data-drawer-remaining>${money(available)}</strong></div>
              </div>
              <div class="payment-progress-bar" data-drawer-progress role="progressbar" aria-label="Progression des règlements" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${initialRatio.toFixed(2)}" aria-valuetext="${initialRatio.toFixed(2)}% réglé — ${money(initialPaid)} réglé, ${money(available)} restant"><span data-drawer-progress-bar style="width:${initialRatio.toFixed(2)}%"></span></div>
              <dl class="payment-drawer-balance-grid">
                <div><dt>Total TTC</dt><dd data-drawer-total>${money(invoice.amount)}</dd></div>
                <div><dt>Déjà réglé</dt><dd data-drawer-paid>${money(invoice.totalPaid)}</dd></div>
                <div><dt>Solde</dt><dd data-drawer-balance>${money(available)}</dd></div>
              </dl>
            </section>
          </section>
          <div class="payment-drawer-quick"><span>Montant du règlement</span><button class="btn btn-secondary" type="button" data-payment-full><span class="material-symbols-rounded">check_circle</span> Régler le solde</button></div>
          <div class="form-fields cols-2">
            <label class="field"><span>Montant</span><input name="amount" type="number" min="0.01" max="${available.toFixed(2)}" step="0.01" value="${available.toFixed(2)}" required /></label>
            <label class="field"><span>Date</span><input name="paymentDate" type="date" value="${today()}" required /></label>
            <label class="field"><span>Mode</span><select name="method" data-searchable="false" required>${METHODS.map(([value, label]) => `<option value="${value}">${label}</option>`).join("")}</select></label>
            <label class="field" data-payment-reference-field hidden><span>Référence</span><input name="reference" maxlength="100" /></label>
            <label class="field full"><span>Note</span><textarea name="note" maxlength="500"></textarea></label>
          </div>
          <p class="payment-dialog-preview" data-payment-preview></p>
          <p class="form-error" data-payment-error hidden></p>
        </div>
        <footer><button class="btn btn-secondary" type="button" data-close>Annuler</button><button class="btn btn-primary payment-submit-primary" type="submit" data-payment-submit><span class="material-symbols-rounded">check</span> Enregistrer le règlement</button></footer>
      </form>`;

    const form = dialog.querySelector("[data-payment-form]");
    const content = dialog.querySelector(".payment-drawer-content");
    const balance = dialog.querySelector(".payment-drawer-balance");
    const historyButton = dialog.querySelector("[data-payment-history-open]");
    const overviewActions = document.createElement("div");
    const registerButton = document.createElement("button");
    const paymentBack = document.createElement("button");
    const history = document.createElement("section");
    overviewActions.className = "payment-overview-actions";
    registerButton.className = "btn btn-primary payment-register-open";
    registerButton.type = "button";
    registerButton.innerHTML = '<span class="material-symbols-rounded" aria-hidden="true">payments</span><span>Enregistrer un règlement</span>';
    paymentBack.className = "payment-view-back";
    paymentBack.type = "button";
    paymentBack.innerHTML = '<span class="material-symbols-rounded" aria-hidden="true">arrow_back</span> Vue d\'ensemble';
    history.className = "payment-drawer-history";
    history.setAttribute("aria-live", "polite");
    history.innerHTML = '<button class="payment-view-back" type="button" data-history-back><span class="material-symbols-rounded" aria-hidden="true">arrow_back</span> Vue d\'ensemble</button><div class="payment-view-heading"><span class="eyebrow">Règlements</span><h3>Historique des paiements</h3><p>Dates, montants et détails de chaque mouvement.</p></div><div class="payment-history-list" data-payment-history-list></div>';
    overviewActions.append(historyButton, registerButton);
    balance.after(overviewActions);
    content.insertBefore(paymentBack, dialog.querySelector(".payment-drawer-quick"));
    content.appendChild(history);
    form.dataset.paymentView = "overview";
    const amount = form.elements.amount;
    const preview = dialog.querySelector("[data-payment-preview]");
    const fullButton = dialog.querySelector("[data-payment-full]");
    const updatePreview = () => {
      const nextRemaining = Math.max(0, Number(amount.max) - U.number(amount.value));
      preview.textContent = `Solde après ce règlement : ${money(nextRemaining)}`;
    };
    const referenceField = form.querySelector("[data-payment-reference-field]");
    const toggleReferenceField = () => {
      const requiresReference = [1, 2, 3].includes(Number(form.elements.method.value));
      referenceField.hidden = !requiresReference;
      if (!requiresReference) form.elements.reference.value = "";
    };

    amount.addEventListener("input", updatePreview);
    form.elements.method.addEventListener("change", toggleReferenceField);
    fullButton.addEventListener("click", () => {
      amount.value = amount.max || available.toFixed(2);
      updatePreview();
      amount.focus();
      amount.select();
    });
    const showView = (view, trigger) => {
      form.dataset.paymentView = view;
      if (view === "history") loadPaymentHistory(history.querySelector("[data-payment-history-list]"), invoice, options);
      const target = view === "payment" ? amount : view === "history" ? history.querySelector("[data-history-back]") : historyButton;
      requestAnimationFrame(() => target.focus());
      trigger?.blur();
    };
    historyButton.addEventListener("click", (event) => showView("history", event.currentTarget));
    registerButton.addEventListener("click", (event) => showView("payment", event.currentTarget));
    paymentBack.addEventListener("click", (event) => showView("overview", event.currentTarget));
    history.querySelector("[data-history-back]").addEventListener("click", (event) => showView("overview", event.currentTarget));
    dialog.querySelectorAll("[data-close]").forEach((button) => button.addEventListener("click", () => dialog.close()));
    loadDrawerBalance(dialog, invoice);
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      await submitPayment(dialog, form, invoice, options);
    });
    document.body.appendChild(dialog);
    dialog.addEventListener("close", () => dialog.remove());
    toggleReferenceField();
    updatePreview();
    dialog.showModal();
    Design.Controls?.refresh(dialog);
    historyButton.focus();
  }

  function updateDrawerBalance(dialog, summary, paymentCount) {
    const paid = dialog.querySelector("[data-drawer-paid]");
    const amount = dialog.querySelector("[name=\"amount\"]");
    const remaining = dialog.querySelector("[data-drawer-remaining]");
    const balance = dialog.querySelector("[data-drawer-balance]");
    const bar = dialog.querySelector("[data-drawer-progress-bar]");
    const progress = dialog.querySelector("[data-drawer-progress]");
    const badge = dialog.querySelector("[data-progress]");
    const count = dialog.querySelector("[data-payment-history-count]");
    const key = progressKey(summary);
    const [label, icon] = PROGRESS[key];

    if (paid) paid.textContent = money(summary.paid);
    if (amount) {
      amount.max = summary.remaining.toFixed(2);
      if (U.number(amount.value) > summary.remaining) amount.value = summary.remaining.toFixed(2);
      amount.dispatchEvent(new Event("input"));
    }
    if (remaining) remaining.textContent = money(summary.remaining);
    if (balance) balance.textContent = money(summary.remaining);
    if (bar) bar.style.width = `${summary.ratio.toFixed(2)}%`;
    if (progress) {
      progress.setAttribute("aria-valuenow", summary.ratio.toFixed(2));
      progress.setAttribute("aria-valuetext", `${summary.ratio.toFixed(2)}% réglé — ${money(summary.paid)} réglé, ${money(summary.remaining)} restant`);
    }
    if (badge) {
      badge.dataset.progress = key;
      badge.innerHTML = `<span class="material-symbols-rounded">${icon}</span>${label}`;
    }
    if (count) {
      count.textContent = String(paymentCount);
      count.setAttribute("aria-label", `${paymentCount} paiement${paymentCount === 1 ? "" : "s"}`);
    }
  }

  async function loadDrawerBalance(dialog, invoice) {
    try {
      const payments = (await Design.Api.documents.payments.list(invoice.id) || []).map(mapPayment);
      updateDrawerBalance(dialog, paymentSummary(invoice, payments), payments.length);
    } catch {
      const count = dialog.querySelector("[data-payment-history-count]");
      if (count) {
        count.textContent = "—";
        count.setAttribute("aria-label", "Nombre de paiements indisponible");
      }
    }
  }

  async function loadPaymentHistory(host, invoice, options) {
    host.replaceChildren(C.emptyState("payments", "Chargement des règlements", "Lecture de l'historique complet..."));
    try {
      const payments = sortPayments((await Design.Api.documents.payments.list(invoice.id) || []).map(mapPayment), true);
      host.replaceChildren(...renderRows(payments, invoice, options));
    } catch (error) {
      host.replaceChildren(C.apiState({ icon: "sync_problem", eyebrow: "Règlements", title: "Historique indisponible", description: error.message }));
    }
  }

  async function submitPayment(dialog, form, invoice, options) {
    const error = form.querySelector("[data-payment-error]");
    error.hidden = true;
    U.setSubmitting(form, true);
    try {
      await Design.Api.documents.payments.create(invoice.id, {
        amount: U.number(form.elements.amount.value),
        paymentDate: form.elements.paymentDate.value,
        method: Number(form.elements.method.value),
        reference: U.optional(form.elements.reference.value),
        note: U.optional(form.elements.note.value),
      });
      dialog.close();
      await refreshAfterMutation(invoice, paymentFlowCopy(invoice).success, options);
    } catch (err) {
      error.textContent = err.message;
      error.hidden = false;
    } finally {
      U.setSubmitting(form, false);
    }
  }

  async function cancelPayment(invoice, payment, options) {
    const confirmed = await C.confirmDelete({
      title: "Annuler ce règlement ?",
      target: `${money(payment.amount)} — ${payment.reference || invoice.ref}`,
      message: "Le mouvement restera dans l'historique mais ne comptera plus dans le solde.",
    });
    if (!confirmed) return;
    try {
      await Design.Api.documents.payments.cancel(invoice.id, payment.id, { reason: "Annulé depuis V2" });
      await refreshAfterMutation(invoice, "Règlement annulé", options);
    } catch (error) {
      C.toast("Annulation impossible", error.message, "error");
    }
  }

  async function refreshAfterMutation(invoice, title, options) {
    await Design.WorkspacePage.reload();
    options.onSaved?.();
    const documentKind = Number(invoice?.natureValue) === 0 ? "purchase" : "sale";
    const refreshed = Design.Store.byId[documentKind](invoice.id);
    const balance = refreshed
      ? money(Math.max(0, Number(refreshed.amount || 0) - Number(refreshed.totalPaid || 0)))
      : money(0);
    try {
      window.sessionStorage.setItem("sopmine-flash-toast", JSON.stringify({
        title,
        message: `Solde : ${balance} restant`,
        type: "success"
      }));
    } catch {/* ignore */}
    if (refreshed) {
      await Promise.all([...document.querySelectorAll("dialog.payment-drawer[open]")].map((drawer) => loadDrawerBalance(drawer, refreshed)));
    }
    if (refreshed && Design.Router.current().includes(invoice.id)) {
      Design.Router.go(`${documentKind}/${invoice.id}`);
    }
  }

  function escapeHtml(value) {
    return String(value || "").replace(/[&<>\"]/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[char]));
  }

  Design.DocumentPayments = { mount, open, mapPayment, progressLabel, isPayableDocument, isPaymentLockedDocument };
})();
