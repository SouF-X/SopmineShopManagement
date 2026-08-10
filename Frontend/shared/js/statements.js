(function () {
  const Design = window.SopmineDesign;
  const U = Design.Utils;
  const C = Design.Components;

  const METHODS = {
    0: "Espèces",
    1: "Chèque",
    2: "Virement",
    3: "Effet",
    4: "Carte",
    Espece: "Espèces",
    Cheque: "Chèque",
    Virement: "Virement",
    Effet: "Effet",
    Carte: "Carte",
  };

  const PROGRESS_OPTIONS = [
    ["", "Tous les statuts"],
    ["Unpaid", "Non réglée"],
    ["Paid", "Réglée"],
  ];

  const COMPANY = {
    name: "STE SOPMINE",
    address: "N° 2 RUE MOHAMED ZERKTOUNI",
    city: "30000 - FES - Maroc",
    phone: "05 35 62 21 85",
    legal: "SARL AU CAPITAL DE 1400000.DH · RC : N 23643 · PATENTE : 1326384 · IF : 04502440 · CNSS : 6550479 · ICE : 001525962000067",
  };

  function queryString(filters = {}) {
    const params = new URLSearchParams();
    if (filters.from) params.set("from", filters.from);
    if (filters.to) params.set("to", filters.to);
    if (filters.paymentProgress) params.set("paymentProgress", filters.paymentProgress);
    const text = params.toString();
    return text ? `?${text}` : "";
  }

  function methodLabel(value) {
    if (value == null || value === "") return "—";
    if (typeof value === "number") return METHODS[value] || "—";
    const numeric = Number(value);
    if (Number.isFinite(numeric) && String(value).trim() !== "") return METHODS[numeric] || METHODS[value] || "—";
    return METHODS[value] || String(value);
  }

  const DOCUMENT_TYPES = {
    2: "Bon de réception",
    3: "Bon de livraison",
    4: "Facture",
    5: "Avoir",
  };

  function documentTypeValue(value) {
    if (value == null || value === "") return null;
    if (typeof value === "number") return Number.isFinite(value) ? value : null;
    const numeric = Number(value);
    if (Number.isFinite(numeric) && String(value).trim() !== "") return numeric;
    const key = String(value).normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    return { bonreception: 2, bonlivraison: 3, facture: 4, avoir: 5 }[key] ?? null;
  }

  function normalizeProgress(value) {
    if (value == null || value === "") return "";
    if (typeof value === "number") return ["unpaid", "partial", "paid", "overdue"][value] || "";
    const key = String(value).normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    return {
      unpaid: "unpaid",
      nonreglee: "unpaid",
      partiallypaid: "partial",
      partial: "partial",
      partiellementreglee: "partial",
      paid: "paid",
      reglee: "paid",
      overdue: "overdue",
      enretard: "overdue",
    }[key] || "";
  }

  function paymentStatus(progress) {
    if (!progress) return null;
    return progress === "paid"
      ? { label: "Réglée", tone: "success", icon: "task_alt" }
      : { label: "Non réglée", tone: "warning", icon: "pending_actions" };
  }

  function accountStatus(statement) {
    return statement.remainingBalance <= 0
      ? { label: "Réglée", tone: "success", icon: "task_alt" }
      : { label: "Non réglée", tone: "warning", icon: "pending_actions" };
  }

  function statusBadge(status, className = "") {
    if (!status) return "";
    const paidClass = status.tone === "success" ? " status--paid" : "";
    return `<span class="status ${status.tone}${paidClass} statement-status ${className}"><span class="material-symbols-rounded" aria-hidden="true">${status.icon}</span>${status.label}</span>`;
  }

  function movementTypeLabel(movement) {
    return movement.isCancelled ? "Règlement annulé" : movement.movementType || (movement.paymentId ? "Règlement" : DOCUMENT_TYPES[movement.documentType] || "Document");
  }

  function mapStatement(dto = {}) {
    const movements = (dto.movements || dto.Movements || []).map((item) => ({
      movementId: String(item.movementId || item.MovementId || ""),
      invoiceId: String(item.invoiceId || item.InvoiceId || ""),
      paymentId: item.paymentId || item.PaymentId || null,
      movementDate: item.movementDate || item.MovementDate || null,
      reference: item.reference || item.Reference || "—",
      method: methodLabel(item.method ?? item.Method),
      documentType: documentTypeValue(item.documentType ?? item.DocumentType),
      movementType: item.movementType || item.MovementType || "",
      documentAmount: Number(item.documentAmount ?? item.DocumentAmount ?? 0),
      balanceImpact: item.balanceImpact != null || item.BalanceImpact != null ? Number(item.balanceImpact ?? item.BalanceImpact) : Number(item.invoicedAmount ?? item.InvoicedAmount ?? 0) - Number(item.paidAmount ?? item.PaidAmount ?? 0),
      paymentProgress: normalizeProgress(item.paymentProgress ?? item.PaymentProgress),
      isInformational: Boolean(item.isInformational ?? item.IsInformational),
      invoicedAmount: Number(item.invoicedAmount ?? item.InvoicedAmount ?? 0),
      paidAmount: Number(item.paidAmount ?? item.PaidAmount ?? 0),
      runningBalance: Number(item.runningBalance ?? item.RunningBalance ?? 0),
      isCancelled: Boolean(item.isCancelled ?? item.IsCancelled),
    }));
    return {
      partyId: String(dto.partyId || dto.PartyId || ""),
      partyName: dto.partyName || dto.PartyName || "Partenaire",
      from: dto.from || dto.From || null,
      to: dto.to || dto.To || null,
      openingBalance: Number(dto.openingBalance ?? dto.OpeningBalance ?? 0),
      totalInvoiced: Number(dto.totalInvoiced ?? dto.TotalInvoiced ?? 0),
      totalCredits: Number(dto.totalCredits ?? dto.TotalCredits ?? 0),
      totalPaid: Number(dto.totalPaid ?? dto.TotalPaid ?? 0),
      remainingBalance: Number(dto.remainingBalance ?? dto.RemainingBalance ?? 0),
      overdueAmount: Number(dto.overdueAmount ?? dto.OverdueAmount ?? 0),
      movements,
    };
  }

  async function render(kind, id, filters = readFiltersFromHash()) {
    const party = kind === "client" ? Design.Store.byId.client(id) : Design.Store.byId.supplier(id);
    if (!party) return Design.Shell.missing("Ce partenaire n'existe plus", kind === "client" ? "clients" : "suppliers");

    const page = document.createElement("section");
    page.className = "view-enter statement-page";
    page.innerHTML = skeleton(kind, party, filters);
    bindFilters(page, kind, id);
    Design.Shell.mount(page, kind === "client" ? "clients" : "suppliers", "Relevé");
    await loadStatement(page, kind, id, filters);
  }

  function skeleton(kind, party, filters) {
    const backRoute = kind === "client" ? `client/${party.id}` : `supplier/${party.id}`;
    return `
      <button class="back-link no-print" type="button" data-route="${backRoute}"><span class="material-symbols-rounded">arrow_back</span> Retour à la fiche</button>
      <section class="record-hero statement-hero">
        <div class="record-identity"><span class="record-monogram ${kind === "client" ? "client" : "supplier"}">${U.initials(party.name)}</span><div class="record-heading"><span class="eyebrow"><span class="material-symbols-rounded">receipt_long</span> Relevé ${kind === "client" ? "client" : "fournisseur"}</span><h1 class="record-title">${escapeHtml(party.name)}</h1><div class="record-meta"><span class="meta-chip">${escapeHtml(party.city || "—")}</span><span class="reference-plate">ICE ${escapeHtml(party.ice || "—")}</span></div></div></div>
        <div class="record-actions no-print"><button class="btn btn-secondary" type="button" data-statement-print><span class="material-symbols-rounded">print</span> Imprimer / PDF</button></div>
      </section>
      <section class="statement-toolbar record-panel no-print"><form class="statement-filters" data-statement-filters><label class="field"><span>Du</span><input name="from" type="date" value="${filters.from || ""}" /></label><label class="field"><span>Au</span><input name="to" type="date" value="${filters.to || ""}" /></label><label class="field"><span>Statut</span><select name="paymentProgress">${PROGRESS_OPTIONS.map(([value, label]) => `<option value="${value}" ${filters.paymentProgress === value ? "selected" : ""}>${label}</option>`).join("")}</select></label><button class="btn btn-primary" type="submit"><span class="material-symbols-rounded">filter_alt</span> Filtrer</button></form></section>
      <section data-statement-host>${C.apiState({ icon: "sync", eyebrow: "Relevé", title: "Chargement du relevé", description: "Calcul des mouvements en cours..." }).outerHTML}</section>`;
  }

  async function loadStatement(page, kind, id, filters) {
    const host = page.querySelector("[data-statement-host]");
    try {
      const dto = kind === "client"
        ? await Design.Api.clients.statement(id, filters)
        : await Design.Api.suppliers.statement(id, filters);
      const statement = mapStatement(dto);
      const party = kind === "client" ? Design.Store.byId.client(id) : Design.Store.byId.supplier(id);
      host.replaceChildren(renderStatement(statement, filters, kind, party));
    } catch (error) {
      host.replaceChildren(C.apiState({ icon: "sync_problem", eyebrow: "Relevé", title: "Relevé indisponible", description: error.message }));
    }
  }

  function periodLabel(filters) {
    if (filters.from && filters.to) return `Du ${U.formatDate(filters.from)} au ${U.formatDate(filters.to)}`;
    if (filters.from) return `À partir du ${U.formatDate(filters.from)}`;
    if (filters.to) return `Jusqu'au ${U.formatDate(filters.to)}`;
    return "Toute la période";
  }

  function statementBalanceLabel(filters) {
    return filters.paymentProgress ? "Solde filtr\u00e9" : "Solde";
  }

  function renderStatement(statement, filters = {}, kind = "client", party = {}) {
    const partyLabel = kind === "client" ? "Client" : "Fournisseur";
    const partyName = party?.name || statement.partyName;
    const partyCity = party?.city || "—";
    const partyIce = party?.ice || "—";
    const generatedAt = new Intl.DateTimeFormat("fr-FR").format(new Date());
    const currentStatus = accountStatus(statement);
    const balanceColumnLabel = statementBalanceLabel(filters);
    const wrap = document.createElement("div");
    wrap.className = "statement-content";
    wrap.innerHTML = `
      <section class="statement-screen">
        <section class="statement-summary-band">
          <article><span>Solde initial</span><strong>${U.money(statement.openingBalance)}</strong></article>
          <article><span>Total facturé</span><strong>${U.money(statement.totalInvoiced)}</strong></article>
          <article><span>Total réglé</span><strong>${U.money(statement.totalPaid)}</strong></article>
          <article class="statement-summary--remaining"><span>Solde restant</span><strong>${U.money(statement.remainingBalance)}</strong>${statusBadge(currentStatus, "statement-account-status")}</article>
          <article class="statement-summary--overdue"><span>En retard</span><strong>${U.money(statement.overdueAmount)}</strong></article>
        </section>
        <section class="record-panel statement-ledger">
          <header class="panel-head">
            <div class="panel-title"><span class="panel-title-icon"><span class="material-symbols-rounded">table</span></span><div><h2>Mouvements</h2><p>${statement.movements.length} ligne${statement.movements.length > 1 ? "s" : ""}</p></div></div>
            <div class="statement-period-label">
              <span class="statement-period-copy">
                <small>Période du relevé</small>
                <strong>${escapeHtml(periodLabel(filters))}</strong>
              </span>
            </div>
          </header>
          <div class="statement-table-wrap"><table class="data-table statement-table"><thead><tr><th>Date</th><th>Nature / Référence</th><th>Mode</th><th>Mouvement</th><th>${balanceColumnLabel}</th></tr></thead><tbody data-screen-statement-body></tbody></table></div>
        </section>
      </section>

      <article class="statement-report statement-print-report">
        <header class="statement-report-head">
          <div class="statement-company">
            <img src="/shared/assets/sopmine-logo.jpeg" alt="SOPMINE" width="982" height="1079" />
            <div>
              <strong>${COMPANY.name}</strong>
              <span>${COMPANY.address}<br />${COMPANY.city}<br />${COMPANY.phone}</span>
            </div>
          </div>
          <div class="statement-document">
            <h2>Relevé de compte</h2>
            <span>Édité le ${escapeHtml(generatedAt)}</span>
          </div>
        </header>

        <section class="statement-party-block">
          <div>
            <span class="statement-report-label">${partyLabel}</span>
            <strong>${escapeHtml(partyName)}</strong>
            <small>${escapeHtml(partyCity)} · ICE ${escapeHtml(partyIce)}</small>
          </div>
          <div class="statement-report-period">
            <span class="statement-report-label">Période du relevé</span>
            <strong>${escapeHtml(periodLabel(filters))}</strong>
          </div>
        </section>

        <section class="statement-balance-band">
          <div>
            <span>Solde restant à payer</span>
            <strong>${U.money(statement.remainingBalance)}</strong>
            ${statusBadge(currentStatus, "statement-account-status")}
          </div>
          <div class="statement-balance-overdue ${statement.overdueAmount > 0 ? "has-overdue" : ""}">
            <span>Dont échu</span>
            <strong>${U.money(statement.overdueAmount)}</strong>
          </div>
        </section>

        <section class="statement-summary-band">
          <article><span>Solde initial</span><strong>${U.money(statement.openingBalance)}</strong></article>
          <article><span>Total facturé</span><strong>${U.money(statement.totalInvoiced)}</strong></article>
          <article><span>Total réglé</span><strong>${U.money(statement.totalPaid)}</strong></article>
        </section>

        <section class="statement-ledger">
          <header class="statement-ledger-heading">
            <div><span class="statement-report-label">Détail du compte</span><h3>Mouvements</h3></div>
            <span>${statement.movements.length} ligne${statement.movements.length > 1 ? "s" : ""}</span>
          </header>
          <div class="statement-table-wrap">
            <table class="statement-table statement-print-table">
              <thead><tr><th>Date</th><th>Nature / Référence</th><th>Mode</th><th>Mouvement</th><th>${balanceColumnLabel}</th></tr></thead>
              <tbody data-print-statement-body></tbody>
            </table>
          </div>
        </section>

        <section class="statement-contact-strip">
          <div><strong>Une question sur ce relevé ?</strong><span>${COMPANY.phone} · ${COMPANY.name}, Fès</span></div>
          <div><strong>Édité le</strong><span>${escapeHtml(generatedAt)}</span></div>
        </section>

        <footer class="statement-report-footer">
          <small>Ce relevé présente la situation du compte à la date d'édition, sous réserve des opérations en cours de traitement.<br />${COMPANY.legal}</small>
        </footer>
      </article>`;

    const screenBody = wrap.querySelector("[data-screen-statement-body]");
    if (statement.openingBalance !== 0) screenBody.appendChild(openingRow(statement.openingBalance));
    if (statement.movements.length) statement.movements.forEach((movement) => screenBody.appendChild(movementRow(movement, balanceColumnLabel)));
    else screenBody.appendChild(emptyMovementRow());

    const printBody = wrap.querySelector("[data-print-statement-body]");
    if (statement.openingBalance !== 0) printBody.appendChild(printOpeningRow(statement.openingBalance));
    if (statement.movements.length) statement.movements.forEach((movement) => printBody.appendChild(printMovementRow(movement, balanceColumnLabel)));
    else printBody.appendChild(printEmptyMovementRow());
    return wrap;
  }

  function openingRow(amount) {
    const row = document.createElement("tr");
    row.className = "statement-opening-row";
    row.innerHTML = `<td data-label="Date">—</td><td data-label="Nature / Référence"><strong class="statement-movement-kind">Solde initial</strong><span class="mono">Ouverture</span></td><td data-label="Mode">—</td><td class="align-right" data-label="Mouvement">—</td><td class="align-right mono" data-label="Solde">${U.money(amount)}</td>`;
    return row;
  }

  function movementAmountLabel(movement) {
    if (movement.isInformational) return "Aucun impact";
    if (movement.paymentId) return movement.paidAmount ? `− ${U.money(movement.paidAmount)}` : "—";
    const impact = Number(movement.balanceImpact || 0);
    if (impact > 0) return `+ ${U.money(Math.abs(impact))}`;
    if (impact < 0) return `− ${U.money(Math.abs(impact))}`;
    return "—";
  }


  function movementRow(movement, balanceColumnLabel) {
    const isPayment = Boolean(movement.paymentId);
    const movementLabel = movementTypeLabel(movement);
    const movementAmount = movementAmountLabel(movement);
    const row = document.createElement("tr");
    row.className = movement.isCancelled ? "is-cancelled" : "";
    row.innerHTML = `<td data-label="Date">${U.formatDate(movement.movementDate)}</td><td data-label="Nature / Référence"><strong class="statement-movement-kind">${movementLabel}</strong><span class="mono">${escapeHtml(movement.reference)}</span></td><td data-label="Mode">${isPayment ? escapeHtml(movement.method) : "—"}</td><td class="align-right" data-label="Mouvement">${movementAmount}</td><td class="align-right mono" data-label="${balanceColumnLabel}">${U.money(movement.runningBalance)}</td>`;
    return row;
  }

  function emptyMovementRow() {
    const row = document.createElement("tr");
    row.innerHTML = `<td colspan="5"><div class="empty-state"><div><span class="material-symbols-rounded">receipt_long</span><h3>Aucun mouvement</h3><p>Aucune facture ou règlement ne correspond aux filtres.</p></div></div></td>`;
    return row;
  }

  function printOpeningRow(amount) {
    const row = document.createElement("tr");
    row.className = "statement-opening-row";
    row.innerHTML = `<td>—</td><td><strong class="statement-movement-kind">Solde initial</strong><span class="mono">Ouverture</span></td><td>—</td><td class="align-right">—</td><td class="align-right mono">${U.money(amount)}</td>`;
    return row;
  }

  function printMovementRow(movement, balanceColumnLabel) {
    const isPayment = Boolean(movement.paymentId);
    const movementLabel = movementTypeLabel(movement);
    const movementAmount = movementAmountLabel(movement);
    const row = document.createElement("tr");
    row.className = movement.isCancelled ? "is-cancelled" : "";
    row.innerHTML = `<td>${U.formatDate(movement.movementDate)}</td><td><strong class="statement-movement-kind">${movementLabel}</strong><span class="mono">${escapeHtml(movement.reference)}</span></td><td>${isPayment ? escapeHtml(movement.method) : "—"}</td><td class="align-right">${movementAmount}</td><td class="align-right mono">${U.money(movement.runningBalance)}</td>`;
    return row;
  }

  function printEmptyMovementRow() {
    const row = document.createElement("tr");
    row.innerHTML = `<td colspan="5"><div class="empty-state"><div><span class="material-symbols-rounded">receipt_long</span><h3>Aucun mouvement</h3><p>Aucune facture ou règlement ne correspond aux filtres.</p></div></div></td>`;
    return row;
  }

  function bindFilters(page, kind, id) {
    page.querySelector("[data-statement-print]").addEventListener("click", () => window.print());
    page.querySelector("[data-statement-filters]").addEventListener("submit", (event) => {
      event.preventDefault();
      const data = new FormData(event.currentTarget);
      const filters = {
        from: data.get("from") || "",
        to: data.get("to") || "",
        paymentProgress: data.get("paymentProgress") || "",
      };
      setHashFilters(filters);
      loadStatement(page, kind, id, filters);
    });
  }

  function readFiltersFromHash() {
    try {
      const url = new URL(location.href.replace("#", "?route="));
      return {
        from: url.searchParams.get("from") || "",
        to: url.searchParams.get("to") || "",
        paymentProgress: url.searchParams.get("paymentProgress") || "",
      };
    } catch {
      return { from: "", to: "", paymentProgress: "" };
    }
  }

  function setHashFilters(filters) {
    const active = Object.fromEntries(Object.entries(filters).filter(([, value]) => value));
    history.replaceState(null, "", `${location.pathname}${location.search}${location.hash.split("?")[0]}${queryString(active)}`);
  }

  function escapeHtml(value) {
    return String(value || "").replace(/[&<>"]/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[char]));
  }

  Design.StatementPage = { render, mapStatement, queryString };
})();
