(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const U = Design.Utils;
  const C = Design.Components;
  const Dom = Design.SupplierDom;

  function showList() {
    const page = Design.SupplierDom.page();
    C.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: "Marques & approvisionnement", icon: "local_shipping", title: "Fournisseurs",
      count: Store.state.suppliers.length,
      description: "Un annuaire relationnel pour retrouver une entreprise et joindre le bon interlocuteur.",
      actionLabel: "Nouveau fournisseur", actionRoute: "supplier-new", secondaryLabel: "Exporter",
    });
    Design.Dom.setText(page, "[data-supplier-contact-count]", `${Store.state.suppliers.reduce((sum, item) => sum + item.contacts.length, 0)} interlocuteurs`);
    const city = page.querySelector("[data-supplier-city]");
    if (window.matchMedia("(max-width: 700px)").matches) {
      city.options[0].textContent = "Villes";
    }
    [...new Set(Store.state.suppliers.map((item) => item.city))].forEach((name) => city.add(new Option(name, name)));
    page.dataset.currentPage = "1";
    const update = (resetPage = true) => {
      if (resetPage) page.dataset.currentPage = "1";
      const query = U.normalizeSearch(page.querySelector("[data-supplier-search]").value);
      const cityValue = city.value;
      const filtered = Store.state.suppliers.filter((supplier) => U.normalizeSearch(`${supplier.name} ${supplier.ice} ${supplier.city} ${supplier.contacts.map((item) => item.name).join(" ")}`).includes(query) && (!cityValue || supplier.city === cityValue));
      const pageSize = 10;
      const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
      const currentPage = Math.min(Number(page.dataset.currentPage || 1), pageCount);
      page.dataset.currentPage = String(currentPage);
      const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
      const list = page.querySelector("[data-supplier-list]");
      list.replaceChildren(...(visible.length ? visible.map(fillCard) : [C.emptyState("local_shipping", "Aucun fournisseur", "Modifiez la recherche ou créez une fiche.")]));
      page.querySelector("[data-supplier-footer]").replaceChildren(C.collectionFooter(filtered.length, "fournisseurs", {
        page: currentPage,
        pageSize,
        onPage: (nextPage) => {
          page.dataset.currentPage = String(nextPage);
          update(false);
        },
      }));
    };
    page.querySelector("[data-supplier-search]").addEventListener("input", () => update());
    city.addEventListener("change", () => update());
    Design.Shell.mount(page, "suppliers");
    update();
  }

  function fillCard(supplier) {
    const card = Design.SupplierDom.card();
    const contact = supplier.contacts[0];
    const productCount = Store.state.products.filter((item) => item.supplierId === supplier.id).length;
    card.querySelector("[data-route]").dataset.route = `supplier/${supplier.id}`;
    Design.Dom.setText(card, "[data-supplier-initials]", U.initials(supplier.name));
    Design.Dom.setText(card, "[data-supplier-name]", supplier.name);
    Design.Dom.setText(card, "[data-supplier-ice]", `ICE ${supplier.ice}`);
    Design.Dom.setText(card, "[data-supplier-city]", supplier.city);
    Design.Dom.setText(card, "[data-supplier-address]", supplier.address);
    Design.Dom.setText(card, "[data-contact-initials]", U.initials(contact?.name || supplier.name));
    Design.Dom.setText(card, "[data-contact-name]", contact?.name || "Aucun contact");
    Design.Dom.setText(card, "[data-contact-role]", contact?.role || "—");
    const phone = contact?.phone || supplier.phone;
    card.querySelector("[data-contact-phone-link]").href = `tel:${phone}`;
    Design.Dom.setText(card, "[data-product-count]", `${productCount} référence${productCount > 1 ? "s" : ""}`);
    Design.Dom.setText(card, "[data-supplier-phone]", supplier.phone);
    return card;
  }

  function showDetail(id) {
    const supplier = Store.byId.supplier(id);
    if (!supplier) return Design.Shell.missing("Ce fournisseur n’existe plus", "suppliers");
    const page = Design.SupplierDom.detail();
    const products = Store.state.products.filter((item) => item.supplierId === id);
    const documents = Store.state.purchases.filter((item) => item.partnerId === id);
    page.querySelectorAll("[data-supplier-initials]").forEach((node) => { node.textContent = U.initials(supplier.name); });
    fillAll(page, "[data-supplier-name]", supplier.name);
    fillAll(page, "[data-supplier-ice]", `ICE ${supplier.ice}`);
    fillAll(page, "[data-supplier-city]", supplier.city);
    fillAll(page, "[data-supplier-phone]", supplier.phone || "—");
    fillAll(page, "[data-supplier-email]", supplier.email || "—");
    fillAll(page, "[data-supplier-website]", supplier.website || "—");
    fillAll(page, "[data-supplier-address]", supplier.address || "—");
    fillAll(page, "[data-contact-count]", `${supplier.contacts.length} contact${supplier.contacts.length > 1 ? "s" : ""}`);
    page.querySelectorAll("[data-edit]").forEach((button) => button.dataset.route = `supplier-edit/${id}`);
    page.querySelector("[data-delete]").addEventListener("click", () => Design.SupplierPage.remove(supplier));
    const statementBtn = page.querySelector("[data-open-statement]");
    const session = window.SopmineAuth?.getSession?.();
    const isEmployee = window.SopmineAuth?.isEmployeeSession?.(session) ?? false;
    if (statementBtn) {
      statementBtn.hidden = isEmployee;
      statementBtn.addEventListener("click", () => Design.Router.go(`supplier-statement/${id}`));
    }
    page.querySelector("[data-new-order]").addEventListener("click", () => {
      Store.state.pendingPartnerId = id;
      Design.Router.go("purchase-new/boncommande");
    });
    page.querySelector("[data-contact-list]").replaceChildren(...supplier.contacts.map(contactCard));
    Design.Dom.setText(page, "[data-linked-count]", `${products.length} références au total`);
    const linkedHost = page.querySelector("[data-linked-products]");
    const linkedSummary = page.querySelector("[data-linked-summary]");
    const visibleProducts = products.slice(-3);
    Design.Dom.setText(page, "[data-visible-count]", visibleProducts.length);
    Design.Dom.setText(page, "[data-linked-total]", products.length);
    linkedSummary.hidden = products.length === 0;
    linkedHost.replaceChildren(...(visibleProducts.length ? visibleProducts.map(productLink) : [C.emptyState("inventory_2", "Aucun produit associé", "Les références liées apparaîtront ici.")]));
    Design.Dom.setText(page, "[data-reference-count]", products.length);
    Design.Dom.setText(page, "[data-document-count]", documents.length);
    Design.Dom.setText(page, "[data-unpaid-count]", documents.filter((item) => item.typeValue === 4 && Number(item.remainingAmount ?? 0) > 0).length);
    Design.Dom.setText(page, "[data-recent-document]", documents[0]?.ref || "—");
    Design.Shell.mount(page, "suppliers", "Détails");
  }

  function contactCard(contact) {
    const card = Dom.contactCard();
    Design.Dom.setText(card, "[data-contact-initials]", U.initials(contact.name));
    Design.Dom.setText(card, "[data-contact-name]", contact.name);
    Design.Dom.setText(card, "[data-contact-role]", contact.role);
    return card;
  }

  function productLink(product) {
    const link = Dom.productLink();
    link.dataset.route = `product/${product.id}`;
    Design.Dom.setText(link, "[data-product-name]", product.name);
    Design.Dom.setText(link, "[data-product-meta]", `${product.reference} · ${U.money(product.purchase)}`);
    return link;
  }

  function fillAll(root, selector, value) {
    root.querySelectorAll(selector).forEach((node) => { node.textContent = value; });
  }

  Design.SupplierList = { showList, showDetail };
})();
