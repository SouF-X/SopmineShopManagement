(function () {
  const Design = window.SopmineDesign;
  const PAGE_SIZE = 10;
  const viewState = {
    family: { query: "", page: 1 },
    unit: { query: "", page: 1 },
  };

  function paginate(items, query = "", requestedPage = 1, pageSize = PAGE_SIZE) {
    const normalizedQuery = Design.Utils.normalizeSearch(query);
    const filtered = normalizedQuery
      ? items.filter((item) => Design.Utils.normalizeSearch(item.name).includes(normalizedQuery))
      : items;
    const pageCount = Math.max(1, Math.ceil(filtered.length / pageSize));
    const page = Math.min(Math.max(Number(requestedPage) || 1, 1), pageCount);
    const start = (page - 1) * pageSize;
    return {
      items: filtered.slice(start, start + pageSize),
      total: filtered.length,
      page,
      pageCount,
    };
  }

  function show() {
    const page = Design.ReferenceDom.page();
    Design.Components.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: "Configuration du catalogue", icon: "tune", title: "Référentiels",
      description: "Les petites listes administratives restent compactes et faciles à contrôler.",
    });
    page.querySelector("[data-family-panel]").replaceChildren(panel("family", "Familles de produits", "Classement principal du catalogue", "category", Design.Store.state.families));
    page.querySelector("[data-unit-panel]").replaceChildren(panel("unit", "Unités de mesure", "Unités proposées pendant la saisie", "straighten", Design.Store.state.units));
    Design.Shell.mount(page, "references");
  }

  function panel(kind, title, description, icon, items) {
    const panelNode = Design.ReferenceDom.panel();
    Design.Dom.setText(panelNode, "[data-reference-icon]", icon);
    Design.Dom.setText(panelNode, "[data-reference-title]", title);
    Design.Dom.setText(panelNode, "[data-reference-description]", description);
    panelNode.querySelector("[data-add]").addEventListener("click", () => Design.ReferenceForms.create(kind));
    const search = panelNode.querySelector("[data-reference-search]");
    const list = panelNode.querySelector("[data-reference-list]");
    const footer = panelNode.querySelector("[data-reference-footer]");
    const state = viewState[kind];

    function updateList() {
      const result = paginate(items, state.query, state.page);
      state.page = result.page;
      list.replaceChildren(...(result.items.length
        ? result.items.map((item) => row(kind, item))
        : [Design.Components.emptyState("search_off", "Aucun résultat", "Modifiez votre recherche pour afficher un référentiel.")]));
      footer.replaceChildren(Design.Components.collectionFooter(
        result.total,
        kind === "family" ? "familles" : "unités",
        {
          page: result.page,
          pageSize: PAGE_SIZE,
          onPage: (page) => {
            state.page = page;
            updateList();
          },
        },
      ));
    }

    search.value = state.query;
    search.addEventListener("input", () => {
      state.query = search.value;
      state.page = 1;
      updateList();
    });
    updateList();
    return panelNode;
  }

  function row(kind, item) {
    const rowNode = Design.ReferenceDom.row();
    rowNode.querySelectorAll("[data-reference-code]").forEach((node) => { node.textContent = item.code.slice(0, 2); });
    Design.Dom.setText(rowNode, "[data-reference-name]", item.name);
    Design.Dom.setText(rowNode, "[data-reference-count]", `${item.count} produit${item.count > 1 ? "s" : ""}`);
    rowNode.querySelector("[data-edit]").addEventListener("click", () => Design.ReferenceForms.update(kind, item));
    rowNode.querySelector("[data-delete]").addEventListener("click", () => Design.ReferenceForms.remove(kind, item));
    return rowNode;
  }

  Design.ReferenceList = { show, paginate };
})();
