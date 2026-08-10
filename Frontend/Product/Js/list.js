(function () {
  const Design = window.SopmineDesign;
  const Dom = Design.ProductDom;
  const Store = Design.Store;
  const U = Design.Utils;
  const C = Design.Components;

  function setAll(root, selector, value) {
    root.querySelectorAll(selector).forEach((node) => { node.textContent = value ?? ""; });
  }

  function fillVisual(product, host) {
    const image = host.querySelector("[data-product-image]");
    const icon = host.querySelector("[data-product-icon]");
    const hasImage = Boolean(product.imageUrl);
    image.hidden = !hasImage;
    icon.hidden = hasImage;
    if (hasImage) image.src = product.imageUrl;
    else icon.textContent = product.icon;
  }

  function showList() {
    const state = Store.state;
    const page = Dom.page();
    if (window.matchMedia('(max-width: 1199px)').matches) state.productView = 'grid';
    C.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: "Catalogue sanitaire",
      icon: "bathroom",
      title: "Produits",
      count: state.products.length,
      description: "Sanitaires, robinetterie, filtration et raccords réunis dans un catalogue clair pour le comptoir.",
      actionLabel: "Nouveau produit",
      actionRoute: "product-new",
      secondaryLabel: "Exporter",
    });

    page.querySelector("[data-product-count]").textContent = state.products.length;
    const family = page.querySelector("[data-product-family]");
    const familyNames = [...new Set([
      ...state.families.map((item) => item.name),
      ...state.products.map((item) => item.family),
    ])];
    familyNames.forEach((name) => family.add(new Option(name, name)));
    page.querySelectorAll("[data-view]").forEach((button) => button.classList.toggle("is-active", button.dataset.view === state.productView));

    page.dataset.currentPage = "1";
    const update = (resetPage = true) => {
      if (resetPage) page.dataset.currentPage = "1";
      updateResults(page);
    };
    page.querySelector("[data-product-search]").addEventListener("input", () => update());
    family.addEventListener("change", () => update());
    page.querySelector("[data-product-stock]").addEventListener("change", () => update());
    page.querySelectorAll("[data-view]").forEach((button) => button.addEventListener("click", () => {
      state.productView = button.dataset.view;
      page.querySelectorAll("[data-view]").forEach((item) => item.classList.toggle("is-active", item === button));
      update();
    }));
    page.querySelectorAll("[data-quick-stock]").forEach((button) => button.addEventListener("click", () => {
      page.querySelector("[data-product-stock]").value = button.dataset.quickStock;
      update();
    }));

    Design.Shell.mount(page, "products");
    updateResults(page);
  }
  function updateResults(page) {
    const query = U.normalizeSearch(page.querySelector("[data-product-search]").value);
    const family = page.querySelector("[data-product-family]").value;
    const stock = page.querySelector("[data-product-stock]").value;
    const filtered = Store.state.products.filter((product) => {
      const supplier = Store.byId.supplier(product.supplierId);
      const searchable = U.normalizeSearch(`${product.name} ${product.reference} ${product.family} ${supplier?.name || ""}`);
      return searchable.includes(query) && (!family || product.family === family) && (!stock || U.stock(product).tone === stock);
    });
    const pageSize = 10;
    const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
    const currentPage = Math.min(Number(page.dataset.currentPage || 1), pageCount);
    page.dataset.currentPage = String(currentPage);
    const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);
    const host = page.querySelector("[data-product-results]");
    if (!visible.length) {
      host.replaceChildren(C.emptyState("search_off", "Aucun produit trouvé", "Modifiez votre recherche ou vos filtres."));
    } else if (Store.state.productView === "grid") {
      host.className = "card-grid";
      host.replaceChildren(...visible.map(fillCard));
    } else {
      host.className = "";
      const table = Dom.table();
      table.querySelector("[data-product-table-body]").replaceChildren(...visible.map(fillRow));
      host.replaceChildren(table);
    }
    page.querySelector("[data-product-footer]").replaceChildren(C.collectionFooter(filtered.length, "produits", {
      page: currentPage,
      pageSize,
      onPage: (nextPage) => {
        page.dataset.currentPage = String(nextPage);
        updateResults(page);
        page.querySelector("[data-product-results]").scrollIntoView({ behavior: "auto", block: "start" });
      },
    }));
  }

  function fillCard(product) {
    const card = Dom.card();
    const supplier = Store.byId.supplier(product.supplierId);
    const stock = U.stock(product);
    card.dataset.route = `product/${product.id}`;
    fillVisual(product, card.querySelector("[data-product-visual]"));
    card.querySelector("[data-product-status]").replaceWith(C.status(stock.label));
    Design.Dom.setText(card, "[data-product-family]", product.family);
    Design.Dom.setText(card, "[data-product-name]", product.name);
    Design.Dom.setText(card, "[data-product-reference]", product.reference);
    Design.Dom.setText(card, "[data-product-supplier]", supplier?.name || "Sans fournisseur");
    Design.Dom.setText(card, "[data-product-stock]", `${product.quantity} ${product.unit.toLowerCase()}`);
    Design.Dom.setText(card, "[data-product-sale]", U.money(product.sale));
    return card;
  }

  function fillRow(product) {
    const row = Dom.row();
    const supplier = Store.byId.supplier(product.supplierId);
    const stock = U.stock(product);
    const margin = product.purchase > 0 ? ((product.sale / (1 + product.vat / 100) - product.purchase) / product.purchase) * 100 : 0;
    row.dataset.open = `product/${product.id}`;
    row.querySelector("[data-route]").dataset.route = `product/${product.id}`;
    fillVisual(product, row.querySelector("[data-product-visual]"));
    Design.Dom.setText(row, "[data-product-name]", product.name);
    Design.Dom.setText(row, "[data-product-reference]", product.reference);
    Design.Dom.setText(row, "[data-product-family]", product.family);
    Design.Dom.setText(row, "[data-product-unit]", product.unit);
    Design.Dom.setText(row, "[data-product-supplier]", supplier?.name || "Sans fournisseur");
    Design.Dom.setText(row, "[data-product-stock]", `${product.quantity} ${product.unit.toLowerCase()}`);
    Design.Dom.setText(row, "[data-product-minimum]", `Min. ${product.minimum} · ${stock.label}`);
    row.querySelector("[data-stock-cell]").classList.add(stock.tone);
    row.querySelector("[data-stock-rail]").style.setProperty("--stock-ratio", stock.ratio);
    Design.Dom.setText(row, "[data-product-purchase]", U.money(product.purchase));
    Design.Dom.setText(row, "[data-product-sale]", U.money(product.sale));
    Design.Dom.setText(row, "[data-product-margin]", `${margin.toFixed(1)} %`);
    return row;
  }

  function showDetail(id) {
    const product = Store.byId.product(id);
    if (!product) return Design.Shell.missing("Ce produit n’existe plus", "products");
    const page = Dom.detail();
    const supplier = Store.byId.supplier(product.supplierId);
    const stock = U.stock(product);
    const saleHt = product.sale / (1 + product.vat / 100 || 1);
    const margin = product.purchase > 0 ? ((saleHt - product.purchase) / product.purchase) * 100 : 0;
    fillVisual(product, page.querySelector("[data-product-visual]"));
    setAll(page, "[data-product-name]", product.name);
    setAll(page, "[data-product-reference]", product.reference);
    setAll(page, "[data-product-family]", product.family);
    setAll(page, "[data-product-unit]", product.unit);
    setAll(page, "[data-product-vat]", `TVA ${product.vat} %`);
    setAll(page, "[data-product-margin]", `Marge ${margin.toFixed(1)} %`);
    setAll(page, "[data-product-purchase]", U.money(product.purchase));
    setAll(page, "[data-product-sale]", U.money(product.sale));
    setAll(page, "[data-product-sale-ht]", U.money(saleHt));
    setAll(page, "[data-product-supplier]", supplier?.name || "Non associé");
    setAll(page, "[data-product-stock-label]", stock.label);
    setAll(page, "[data-product-quantity]", product.quantity);
    setAll(page, "[data-product-stock-copy]", `${product.unit.toLowerCase()} disponibles`);
    setAll(page, "[data-product-minimum]", `Min. ${product.minimum}`);
    page.querySelectorAll("[data-product-status]").forEach((node) => node.replaceWith(C.status(stock.label)));
    const meter = page.querySelector("[data-stock-meter]");
    meter.classList.add(stock.tone);
    meter.style.setProperty("--meter", `${Math.max(4, Math.round(stock.ratio * 100))}%`);
    page.querySelector("[data-edit]").dataset.route = `product-edit/${id}`;
    page.querySelector("[data-delete]").addEventListener("click", () => Design.ProductPage.remove(product));
    const supplierPanel = page.querySelector("[data-supplier-panel]");
    supplierPanel.hidden = !supplier;
    if (supplier) {
      page.querySelector("[data-supplier-link]").dataset.route = `supplier/${supplier.id}`;
      Design.Dom.setText(page, "[data-supplier-initials]", U.initials(supplier.name));
      Design.Dom.setText(page, "[data-supplier-name]", supplier.name);
      Design.Dom.setText(page, "[data-supplier-meta]", `${supplier.city} · ${supplier.phone}`);
    }
    Design.Shell.mount(page, "products", "Détails");
  }

  Design.ProductList = { showList, showDetail };
})();
