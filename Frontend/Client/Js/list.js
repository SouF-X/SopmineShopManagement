(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const U = Design.Utils;
  const C = Design.Components;

  function showList() {
    const page = Design.ClientDom.page();
    C.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: "Portefeuille commercial", icon: "domain", title: "Clients", count: Store.state.clients.length,
      description: "Sélectionnez un client pour consulter ses coordonnées et ses documents sans quitter la liste.",
      actionLabel: "Nouveau client", actionRoute: "client-new", secondaryLabel: "Exporter",
    });
    page.dataset.currentPage = "1";
    const update = (resetPage = true) => {
      if (resetPage) page.dataset.currentPage = "1";
      const query = U.normalizeSearch(page.querySelector("[data-client-search]").value);
      const type = page.querySelector("[data-client-type]").value;
      const filtered = Store.state.clients.filter((client) => U.normalizeSearch(`${client.name} ${client.ice} ${client.city} ${client.contact}`).includes(query) && (!type || client.type === type));
      const pageSize = window.matchMedia("(max-width: 767px)").matches ? 5 : 10;
      const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
      const currentPage = Math.min(Number(page.dataset.currentPage || 1), pageCount);
      page.dataset.currentPage = String(currentPage);
      const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
      if (visible.length && !visible.some((client) => client.id === Store.state.selectedClientId)) {
        Store.state.selectedClientId = visible[0].id;
      }
      const isEmpty = visible.length === 0;
      page.querySelector(".crm-workspace").classList.toggle("is-empty", isEmpty);
      page.querySelector("[data-client-list]").replaceChildren(...(isEmpty
        ? [C.emptyState("person", "Aucun client", "Modifiez la recherche ou créez une fiche.")]
        : visible.map(clientRow)));
      page.querySelector("[data-client-footer]").replaceChildren(C.collectionFooter(filtered.length, "clients", {
        page: currentPage,
        pageSize,
        onPage: (nextPage) => {
          page.dataset.currentPage = String(nextPage);
          update(false);
        },
      }));
      if (isEmpty) {
        page.querySelector("[data-client-preview-host]").replaceChildren();
      } else {
        showPreview(page);
      }
    };
    page.querySelector("[data-client-search]").addEventListener("input", () => update());
    page.querySelector("[data-client-type]").addEventListener("change", () => update());
    Design.Shell.mount(page, "clients");
    update();
  }

  function clientRow(client) {
    const row = Design.ClientDom.row();
    row.dataset.clientId = client.id;
    row.classList.toggle("is-selected", client.id === Store.state.selectedClientId);
    Design.Dom.setText(row, "[data-client-initials]", U.initials(client.name));
    Design.Dom.setText(row, "[data-client-name]", client.name);
    Design.Dom.setText(row, "[data-client-meta]", `${client.city} · ${client.type}`);
    Design.Dom.setText(row, "[data-document-count]", Store.state.sales.filter((item) => item.partnerId === client.id).length);
    row.addEventListener("click", () => {
      Store.state.selectedClientId = client.id;
      const page = row.closest(".view-enter");
      page.querySelectorAll("[data-client-id]").forEach((item) => item.classList.toggle("is-selected", item === row));
      showPreview(page);
    });
    return row;
  }

  function showPreview(page) {
    const client = Store.byId.client(Store.state.selectedClientId) || Store.state.clients[0];
    const preview = Design.ClientDom.preview();
    const docs = Store.state.sales.filter((item) => item.partnerId === client.id);
    const isMobile = window.matchMedia("(max-width: 767px)").matches;
    const latestDocuments = docs.slice(0, isMobile ? 1 : 3);
    fillClient(preview, client);
    preview.querySelector("[data-open]").dataset.route = `client/${client.id}`;
    preview.querySelector("[data-new-quote]").addEventListener("click", () => newQuote(client.id));
    preview.querySelector("[data-open-sales]").addEventListener("click", () => openSales(client.id));
    const host = preview.querySelector("[data-document-list]");
    host.replaceChildren(...(latestDocuments.length ? latestDocuments.map(documentLink) : [C.emptyState("description", "Aucun document", "Créez le premier devis de ce client.")]));
    Design.Dom.setText(preview, "[data-document-summary]", docs.length > 3 ? `3 derniers documents · ${docs.length} au total` : `${docs.length} document${docs.length > 1 ? "s" : ""}`);
    page.querySelector("[data-client-preview-host]").replaceChildren(preview);
  }

  function showDetail(id) {
    const client = Store.byId.client(id);
    if (!client) return Design.Shell.missing("Ce client n’existe plus", "clients");
    const page = Design.ClientDom.detail();
    const docs = Store.state.sales.filter((item) => item.partnerId === id);
    fillClient(page, client);
    fillAll(page, "[data-contact-count]", `${client.contacts.length} contact${client.contacts.length > 1 ? "s" : ""}`);
    page.querySelector("[data-edit]").dataset.route = `client-edit/${id}`;
    page.querySelector("[data-delete]").addEventListener("click", () => Design.ClientPage.remove(client));
    page.querySelector("[data-new-quote]").addEventListener("click", () => newQuote(id));
    page.querySelector("[data-open-sales]").addEventListener("click", () => openSales(id));
    page.querySelector("[data-open-statement]")?.addEventListener("click", () => Design.Router.go(`client-statement/${id}`));
    page.querySelector("[data-contact-list]").replaceChildren(...(client.contacts.length ? client.contacts.map(contactCard) : [C.emptyState("person", "Aucun contact", "Ajoutez un interlocuteur depuis la modification.")]));
    page.querySelector("[data-document-list]").replaceChildren(...(docs.length ? docs.slice(0, 3).map(documentLink) : [C.emptyState("description", "Aucun document", "Créez le premier devis de ce client.")]));
    Design.Dom.setText(page, "[data-open-count]", client.openDocs);
    Design.Dom.setText(page, "[data-invoiced-total]", U.money(docs.filter((item) => item.type === "Facture client").reduce((sum, item) => sum + item.amount, 0)));
    Design.Dom.setText(page, "[data-document-count]", docs.length);
    Design.Shell.mount(page, "clients", "Détails");
  }

  function fillClient(root, client) {
    fillAll(root, "[data-client-initials]", U.initials(client.name));
    fillAll(root, "[data-client-name]", client.name);
    fillAll(root, "[data-client-type]", client.type);
    fillAll(root, "[data-client-city]", client.city);
    fillAll(root, "[data-client-ice]", client.ice);
    fillAll(root, "[data-client-phone]", client.phone || "—");
    fillAll(root, "[data-client-address]", client.address || "—");
    fillAll(root, "[data-contact-name]", client.contact);
    fillAll(root, "[data-contact-role]", client.role);
  }

  function contactCard(contact) {
    const card = Design.ClientDom.contactCard();
    Design.Dom.setText(card, "[data-contact-initials]", U.initials(contact.name));
    Design.Dom.setText(card, "[data-contact-name]", contact.name);
    Design.Dom.setText(card, "[data-contact-role]", contact.role);
    return card;
  }

  function documentLink(documentItem) {
    const link = Design.ClientDom.documentLink();
    link.dataset.route = `sale/${documentItem.id}`;
    Design.Dom.setText(link, "[data-document-reference]", documentItem.ref);
    Design.Dom.setText(link, "[data-document-meta]", `${documentItem.type} · ${documentItem.date}`);
    link.querySelector("[data-document-status]").replaceWith(C.status(documentItem.status));
    return link;
  }

  function newQuote(clientId) {
    Store.state.pendingPartnerId = clientId;
    Design.Router.go("sale-new/devis");
  }

  function openSales(clientId) {
    Store.state.pendingDocumentPartnerId = clientId;
    const latest = Store.state.sales.find((documentItem) => documentItem.partnerId === clientId);
    const section = Design.DocumentData.sections.sales.find((item) => item.type === latest?.type)
      || Design.DocumentData.sections.sales[0];
    Design.Router.go(`sales/${section.key}`);
  }

  function fillAll(root, selector, value) {
    root.querySelectorAll(selector).forEach((node) => { node.textContent = value; });
  }

  Design.ClientList = { showList, showDetail };
})();
