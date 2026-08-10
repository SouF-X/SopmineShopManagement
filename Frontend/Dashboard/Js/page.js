(function () {
  const Design = window.SopmineDesign;
  const U = Design.Utils;
  let currentPeriod = "week";

  function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text != null) node.textContent = text;
    return node;
  }

  function icon(name) {
    const node = el("span", "material-symbols-rounded", name);
    node.setAttribute("aria-hidden", "true");
    return node;
  }

  function relativeTime(value) {
    if (!value) return "Date indisponible";
    const minutes = Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 60000));
    if (minutes < 2) return "À l’instant";
    if (minutes < 60) return `Il y a ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `Il y a ${hours} h`;
    const days = Math.floor(hours / 24);
    return days === 1 ? "Hier" : `Il y a ${days} jours`;
  }

  function periodLabel(period) {
    return { today: "Aujourd’hui", week: "7 derniers jours", month: "Ce mois" }[period];
  }

  function renderChart(host, data) {
    const days = data.salesTrend;
    const width = 720;
    const height = 178;
    const padX = 12;
    const top = 16;
    const bottom = 34;
    const usableHeight = height - top - bottom;
    const max = Math.max(...days.map((day) => day.value), 1);
    const step = days.length > 1 ? (width - padX * 2) / (days.length - 1) : width - padX * 2;
    const points = days.map((day, index) => ({
      ...day,
      x: padX + index * step,
      y: top + usableHeight - (day.value / max) * usableHeight,
    }));
    const line = points.map((point, index) => `${index ? "L" : "M"}${point.x.toFixed(1)},${point.y.toFixed(1)}`).join(" ");
    const area = `${line} L${points.at(-1).x.toFixed(1)},${height - bottom} L${points[0].x.toFixed(1)},${height - bottom} Z`;
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", `${periodLabel(data.period)} : ${U.money(data.periodRevenue)} sur ${data.periodInvoiceCount} facture${data.periodInvoiceCount > 1 ? "s" : ""}.`);
    svg.innerHTML = `<path class="pulse-chart-baseline" d="M${padX},${height - bottom} H${width - padX}"/><path class="pulse-chart-area" d="${area}"/><path class="pulse-chart-line" d="${line}"/>`;
    points.forEach((point, index) => {
      const showLabel = days.length <= 7 || index === 0 || index === days.length - 1 || index % 5 === 0;
      const group = document.createElementNS("http://www.w3.org/2000/svg", "g");
      group.classList.add("pulse-chart-point");
      group.innerHTML = `<circle cx="${point.x}" cy="${point.y}" r="${point.value ? 4.5 : 2.5}"/><title>${new Intl.DateTimeFormat("fr-FR", { day: "numeric", month: "short" }).format(point.date)} — ${U.money(point.value)}</title>${showLabel ? `<text x="${point.x}" y="${height - 10}" text-anchor="middle">${new Intl.DateTimeFormat("fr-FR", days.length > 7 ? { day: "numeric" } : { weekday: "short" }).format(point.date).replace(".", "")}</text>` : ""}`;
      svg.append(group);
    });
    host.append(svg);
  }

  function renderPriorities(host, alerts) {
    if (!alerts.length) {
      const healthy = el("div", "pulse-healthy");
      healthy.append(icon("task_alt"), el("strong", "", "Rien d’urgent"), el("span", "", "Stocks et encaissements ne demandent aucune action immédiate."));
      host.append(healthy);
      return;
    }
    alerts.slice(0, 4).forEach((alert, index) => {
      const row = el("button", `pulse-priority is-${alert.kind}`);
      row.type = "button";
      row.dataset.route = alert.product ? `product/${alert.product.id}` : "sales/facture";
      const order = el("span", "pulse-priority-order", String(index + 1).padStart(2, "0"));
      const copy = el("span", "pulse-priority-copy");
      if (alert.product) {
        copy.append(el("strong", "", alert.kind === "out" ? "Remettre en stock" : "Anticiper la rupture"), el("span", "", alert.product.name), el("small", "", `${alert.product.quantity} disponible · seuil ${alert.product.minimum}`));
      } else {
        copy.append(el("strong", "", "Suivre les encaissements"), el("span", "", `${alert.count} facture${alert.count > 1 ? "s ouvertes" : " ouverte"}`), el("small", "", `${U.money(alert.amount)} à récupérer`));
      }
      row.append(order, copy, icon("north_east"));
      host.append(row);
    });
  }

  function renderActivity(host, items) {
    if (!items.length) {
      host.append(el("p", "pulse-empty", "L’activité apparaîtra ici dès la première opération."));
      return;
    }
    items.slice(0, 6).forEach((item) => {
      const row = el("button", "pulse-ledger-row");
      row.type = "button";
      row.dataset.route = item.route;
      const marker = el("span", `pulse-ledger-marker is-${item.kind}`);
      marker.append(icon(item.kind === "sale" ? "receipt_long" : item.kind === "purchase" ? "local_shipping" : "inventory_2"));
      const copy = el("span", "pulse-ledger-copy");
      copy.append(el("strong", "", item.label), el("small", "", item.detail));
      const meta = el("span", "pulse-ledger-meta");
      if (item.amount) meta.append(el("strong", "mono", U.money(item.amount)));
      meta.append(el("small", "", relativeTime(item.createdAt)));
      row.append(marker, copy, meta, icon("chevron_right"));
      host.append(row);
    });
  }

  function render(period = currentPeriod) {
    currentPeriod = period;
    const data = window.SopmineDashboardModel.buildDashboard(Design.Store.state, period);
    const session = window.SopmineAuth?.getSession?.();
    const isEmployee = window.SopmineAuth?.isEmployeeSession?.(session) ?? false;
    const email = window.SopmineAuth?.getSessionEmail?.(session) || "équipe";
    const person = email.split("@")[0].replace(/[._-]+/g, " ");
    const page = el("div", "pulse-page view-enter");
    page.innerHTML = `
      <section class="dashboard-welcome">
        <span class="dashboard-welcome-avatar" data-welcome-initials></span>
        <div class="dashboard-welcome-copy">
          <span class="dashboard-welcome-date" data-welcome-date></span>
          <h1>Bonjour, <span data-welcome-name></span></h1>
          <p>Voici l’activité de votre point de vente.</p>
        </div>
        <div class="dashboard-welcome-tools">
          <div class="pulse-period" role="group" aria-label="Période du tableau de bord">
            <button type="button" data-period="today">Aujourd’hui</button>
            <button type="button" data-period="week">7 jours</button>
            <button type="button" data-period="month">Ce mois</button>
          </div>
          <button class="btn btn-primary dashboard-bl-action" type="button" data-route="sale-new/bonlivraison" aria-label="Nouveau bon de livraison"><span class="material-symbols-rounded" aria-hidden="true">add</span><span class="dashboard-bl-label dashboard-bl-label-full">Nouveau bon de livraison</span><span class="dashboard-bl-label dashboard-bl-label-compact">Nouveau BL</span></button>
        </div>
      </section>
      <div class="dashboard-columns">
        <div class="dashboard-column dashboard-column-main">
          <section class="pulse-section pulse-sales">
            <header class="pulse-section-head"><div><h2>Mouvement des ventes</h2><p>Factures réellement enregistrées sur la période.</p></div><button type="button" data-route="sales/facture">Toutes les ventes <span class="material-symbols-rounded">arrow_forward</span></button></header>
            <div class="pulse-chart" data-chart></div>
            <footer class="pulse-sales-foot">
              <div data-strongest-wrap><span>Journée la plus forte</span><strong data-strongest-day></strong></div>
              <button class="pulse-last-sale" type="button" data-last-sale><span class="pulse-last-icon"><span class="material-symbols-rounded">receipt_long</span></span><span><small>Dernière facture</small><strong data-last-client></strong></span><span class="mono" data-last-amount></span><span class="material-symbols-rounded">north_east</span></button>
            </footer>
          </section>

          <section class="pulse-section pulse-ledger">
            <header class="pulse-section-head"><div><h2>Journal de l’activité</h2><p>Les dernières opérations du système, dans l’ordre.</p></div><span class="material-symbols-rounded pulse-head-symbol">history</span></header>
            <div data-activity></div>
          </section>
        </div>

        <div class="dashboard-column dashboard-column-side">
          <section class="pulse-section pulse-queue">
            <header class="pulse-section-head"><div><h2>À faire maintenant</h2><p>Classé par impact opérationnel.</p></div><span class="pulse-count" data-priority-count></span></header>
            <div class="pulse-priorities" data-priorities></div>
          </section>

          <aside class="pulse-command">
            <div class="pulse-command-head"><span class="material-symbols-rounded">bolt</span><div><strong>Passer à l’action</strong><small>Les raccourcis du comptoir</small></div></div>
            <div class="pulse-command-actions">
              <button type="button" data-route="sale-new/devis"><span class="material-symbols-rounded">request_quote</span><span><strong>Nouveau devis</strong><small>Offre client</small></span></button>
              <button type="button" data-route="purchase-new/boncommande" data-purchase-action><span class="material-symbols-rounded">shopping_cart</span><span><strong>Commander</strong><small>Bon fournisseur</small></span></button>
              <button type="button" data-route="product-new"><span class="material-symbols-rounded">add_box</span><span><strong>Ajouter un produit</strong><small>Catalogue</small></span></button>
              <button type="button" data-route="client-new"><span class="material-symbols-rounded">person_add</span><span><strong>Nouveau client</strong><small>Partenaire</small></span></button>
            </div>
            <div class="pulse-base"><span><strong data-products></strong> produits</span><span><strong data-clients></strong> clients</span><span><strong data-suppliers></strong> fournisseurs</span></div>
          </aside>
        </div>
      </div>`;

    page.querySelector("[data-welcome-initials]").textContent = U.initials(person);
    page.querySelector("[data-welcome-name]").textContent = person;
    page.querySelector("[data-welcome-date]").textContent = new Intl.DateTimeFormat("fr-FR", { weekday: "long", day: "numeric", month: "long" }).format(new Date());
    page.querySelector("[data-priority-count]").textContent = data.alerts.length ? `${data.alerts.length} priorités` : "À jour";
    page.querySelector("[data-products]").textContent = data.counts.products;
    page.querySelector("[data-clients]").textContent = data.counts.clients;
    page.querySelector("[data-suppliers]").textContent = data.counts.suppliers;
    page.querySelectorAll("[data-period]").forEach((button) => {
      button.classList.toggle("is-active", button.dataset.period === period);
      button.setAttribute("aria-pressed", String(button.dataset.period === period));
      button.addEventListener("click", () => render(button.dataset.period));
    });

    renderChart(page.querySelector("[data-chart]"), data);
    const strongestWrap = page.querySelector("[data-strongest-wrap]");
    if (data.strongestDay) {
      page.querySelector("[data-strongest-day]").textContent = `${new Intl.DateTimeFormat("fr-FR", { weekday: "long", day: "numeric" }).format(data.strongestDay.date)} · ${U.money(data.strongestDay.value)}`;
    } else {
      strongestWrap.querySelector("span").textContent = "Période calme";
      strongestWrap.querySelector("strong").textContent = "Aucune facture enregistrée";
    }

    const lastSale = page.querySelector("[data-last-sale]");
    if (data.latestSale) {
      lastSale.dataset.route = `sale/${data.latestSale.id}`;
      page.querySelector("[data-last-client]").textContent = `${data.latestSale.partnerName} · ${data.latestSale.ref}`;
      page.querySelector("[data-last-amount]").textContent = U.money(data.latestSale.amount);
    } else {
      lastSale.disabled = true;
      page.querySelector("[data-last-client]").textContent = "Aucune facture client";
      page.querySelector("[data-last-amount]").textContent = "—";
    }

    renderPriorities(page.querySelector("[data-priorities]"), data.alerts);
    renderActivity(page.querySelector("[data-activity]"), data.activity);
    if (isEmployee) page.querySelector("[data-purchase-action]")?.remove();
    Design.Shell.mount(page, "dashboard");
  }

  Design.DashboardPage = { render };
})();