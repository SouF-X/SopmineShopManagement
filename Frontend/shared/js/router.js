(function () {
  const Design = window.SopmineDesign;
  let started = false;

  const featureVersion = "20260724payments";
  const featurePaths = {
    Dashboard: `/Dashboard/index.html?ui=${featureVersion}`,
    Product: `/Product/index.html?ui=${featureVersion}`,
    Supplier: `/Supplier/index.html?ui=${featureVersion}`,
    Client: `/Client/index.html?ui=${featureVersion}`,
    Document: `/Document/index.html?ui=${featureVersion}`,
    Reference: `/Reference/index.html?ui=${featureVersion}`,
    Settings: `/Settings/index.html?ui=${featureVersion}`,
  };

  function current() {
    return location.hash.slice(1) || document.body.dataset.defaultRoute || "products";
  }

  function featureFor(route) {
    const head = route.split("/")[0];
    if (head === "dashboard") return "Dashboard";
    if (["products", "product", "product-new", "product-edit"].includes(head)) return "Product";
    if (["suppliers", "supplier", "supplier-new", "supplier-edit", "supplier-statement"].includes(head)) return "Supplier";
    if (["clients", "client", "client-new", "client-edit", "client-statement"].includes(head)) return "Client";
    if (["purchases", "purchase", "purchase-new", "purchase-edit", "sales", "sale", "sale-new", "sale-edit"].includes(head)) return "Document";
    if (head === "references") return "Reference";
    if (head === "settings") return "Settings";
    return "Product";
  }

  function go(route) {
    const targetFeature = featureFor(route);
    const currentFeature = document.documentElement.dataset.feature;
    if (currentFeature && currentFeature !== targetFeature.toLowerCase()) {
      location.assign(`${featurePaths[targetFeature]}#${route}`);
      return;
    }
    if (current() === route) location.reload();
    else location.assign(`${featurePaths[targetFeature]}#${route}`);
  }

  function render() {
    if (!Design.Store.state.ready) return;
    const route = current();
    const [head, id] = route.split("/");
    const session = window.SopmineAuth?.getSession?.();
    const isEmployee = window.SopmineAuth?.isEmployeeSession?.(session) ?? false;
    const role = window.SopmineAuth?.getSessionRole?.(session) || "";
    const isAdmin = !isEmployee && role.toLowerCase() === "admin";
    if (isEmployee && (route.startsWith("purchase") || head === "supplier-statement")) {
      return go("sales/devis");
    }
    if (head === "settings" && !isAdmin) {
      return go("products");
    }

    if (route === "dashboard") Design.DashboardPage.render();
    else if (route === "products") Design.ProductPage.list();
    else if (head === "product" && id) Design.ProductPage.detail(id);
    else if (route === "product-new") Design.ProductPage.form();
    else if (head === "product-edit" && id) Design.ProductPage.form(id);
    else if (route === "suppliers") Design.SupplierPage.list();
    else if (head === "supplier" && id) Design.SupplierPage.detail(id);
    else if (head === "supplier-statement" && id) Design.StatementPage.render("supplier", id);
    else if (route === "supplier-new") Design.SupplierPage.form();
    else if (head === "supplier-edit" && id) Design.SupplierPage.form(id);
    else if (route === "clients") Design.ClientPage.list();
    else if (head === "client" && id) Design.ClientPage.detail(id);
    else if (head === "client-statement" && id) Design.StatementPage.render("client", id);
    else if (route === "client-new") Design.ClientPage.form();
    else if (head === "client-edit" && id) Design.ClientPage.form(id);
    else if (route === "purchases") Design.DocumentPage.list("purchases", "boncommande");
    else if (route === "purchases/lecture-ia") Design.DocumentPage.aiWorkspace();
    else if (head === "purchases") Design.DocumentPage.list("purchases", id);
    else if (head === "purchase" && id) Design.DocumentPage.detail("purchase", id);
    else if (route === "purchase-new") Design.DocumentPage.form("purchase", "boncommande");
    else if (head === "purchase-new") Design.DocumentPage.form("purchase", id);
    else if (head === "purchase-edit" && id) Design.DocumentPage.form("purchase", null, id);
    else if (route === "sales") Design.DocumentPage.list("sales", "devis");
    else if (head === "sales") Design.DocumentPage.list("sales", id);
    else if (head === "sale" && id) Design.DocumentPage.detail("sale", id);
    else if (route === "sale-new") Design.DocumentPage.form("sale", "devis");
    else if (head === "sale-new") Design.DocumentPage.form("sale", id);
    else if (head === "sale-edit" && id) Design.DocumentPage.form("sale", null, id);
    else if (route === "references") Design.ReferencePage.render();
    else if (head === "settings" && ["users", "numbering"].includes(id)) Design.SettingsPage.render(id);
    else go("products");
  }

  function start() {
    if (!started) {
      window.addEventListener("hashchange", () => location.reload());
      started = true;
    }
    render();
  }

  Design.Router = { current, go, render, start };
})();
