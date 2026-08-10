(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const U = Design.Utils;

  function render(id) {
    const product = id ? Store.byId.product(id) : null;
    if (id && !product) return Design.Shell.missing("Ce produit n’existe plus", "products");
    const editing = Boolean(product);
    const page = Design.ProductDom.form();
    const form = page.querySelector("[data-product-form]");
    const backRoute = editing ? `product/${id}` : "products";
    page.querySelectorAll("[data-back]").forEach((button) => button.dataset.route = backRoute);
    Design.Dom.setText(page, "[data-form-title]", editing ? "Modifier le produit" : "Créer un produit");
    Design.Dom.setText(page, "[data-submit-label]", editing ? "Enregistrer" : "Créer le produit");
    page.querySelector("[data-form-icon]").textContent = editing ? "edit" : "add_box";

    fillOptions(form.querySelector('[name="family"]'), Store.state.families, product?.family, "Sélectionner une famille…");
    fillOptions(form.querySelector('[name="unit"]'), Store.state.units, product?.unit, "Sélectionner une unité…");
    fillSuppliers(form.querySelector('[name="supplier"]'), product?.supplierId);
    const values = product || { name: "", reference: "", imageUrl: "", quantity: 0, minimum: 5, purchase: 0, vat: 20, margin: 0, sale: 0 };
    set(form, "name", values.name);
    set(form, "reference", values.reference);
    set(form, "imageUrl", values.imageUrl);
    set(form, "quantity", values.quantity);
    set(form, "minimum", values.minimum);
    set(form, "purchase", values.purchase);
    set(form, "vat", values.vat);
    set(form, "margin", values.margin);
    setupImageUpload(form);

    const updateSummary = () => {
      const purchase = U.number(form.elements.purchase.value);
      const vat = U.number(form.elements.vat.value);
      const margin = U.number(form.elements.margin.value);
      const saleHt = purchase * (1 + margin / 100);
      const saleTtc = saleHt * (1 + vat / 100);
      const marginLabel = `${margin.toFixed(1)} %`;
      Design.Dom.setText(page, "[data-sale-ht]", U.money(saleHt));
      Design.Dom.setText(page, "[data-sale-ttc]", U.money(saleTtc));
      Design.Dom.setText(page, "[data-summary-purchase]", U.money(purchase));
      Design.Dom.setText(page, "[data-summary-sale]", U.money(saleHt));
      Design.Dom.setText(page, "[data-summary-vat]", `${vat} %`);
      Design.Dom.setText(page, "[data-summary-margin]", marginLabel);
    };
    let lastEditedPurchase = "ht";
    const syncPurchaseTtc = () => {
      const purchase = U.number(form.elements.purchase.value);
      const vat = U.number(form.elements.vat.value);
      form.elements.purchaseTtc.value = Number((purchase * (1 + vat / 100)).toFixed(2));
    };
    const syncPurchaseHt = () => {
      const purchaseTtc = U.number(form.elements.purchaseTtc.value);
      const vat = U.number(form.elements.vat.value);
      form.elements.purchase.value = Number((purchaseTtc / (1 + vat / 100)).toFixed(2));
    };
    syncPurchaseTtc();
    form.elements.purchase.addEventListener("input", () => {
      lastEditedPurchase = "ht";
      syncPurchaseTtc();
      updateSummary();
    });
    form.elements.purchaseTtc.addEventListener("input", () => {
      lastEditedPurchase = "ttc";
      syncPurchaseHt();
      updateSummary();
    });
    form.elements.vat.addEventListener("input", () => {
      if (lastEditedPurchase === "ttc") syncPurchaseHt();
      else syncPurchaseTtc();
      updateSummary();
    });
    form.elements.margin.addEventListener("input", updateSummary);
    form.addEventListener("submit", (event) => save(event, product));
    updateSummary();
    Design.Shell.mount(page, "products", editing ? "Modifier" : "Ajouter");
  }

  function fillOptions(select, items, selected, placeholder) {
    select.replaceChildren(new Option(placeholder, ""));
    items.forEach((item) => select.add(new Option(item.name, item.name, false, item.name === selected)));
  }

  function fillSuppliers(select, selected) {
    select.replaceChildren(new Option("Sans fournisseur", ""));
    Store.state.suppliers.forEach((supplier) => select.add(new Option(`${supplier.name} · ${supplier.city}`, supplier.id, false, supplier.id === selected)));
  }

  function set(form, name, value) {
    form.elements[name].value = value ?? "";
  }

  function setupImageUpload(form) {
    const imageValue = form.elements.imageUrl;
    const fileInput = form.querySelector("[data-image-file]");
    const selectButton = form.querySelector("[data-image-select]");
    const preview = form.querySelector("[data-image-preview]");
    const previewImage = form.querySelector("[data-image-preview-image]");
    const removeButton = form.querySelector("[data-image-remove]");
    const maxBytes = 700 * 1024;
    const supportedTypes = new Set(["image/png", "image/jpeg", "image/gif", "image/webp"]);

    const syncPreview = () => {
      const hasImage = Boolean(imageValue.value);
      preview.hidden = !hasImage;
      selectButton.hidden = hasImage;
      previewImage.removeAttribute("src");
      if (hasImage) previewImage.src = imageValue.value;
    };

    selectButton.addEventListener("click", () => fileInput.click());
    fileInput.addEventListener("change", () => {
      const [file] = fileInput.files;
      if (!file) return;
      if (!supportedTypes.has(file.type) || file.size > maxBytes) {
        Design.Components.toast("Image non importée", "Choisissez une image PNG, JPG, GIF ou WebP de 700 Ko maximum.", "error");
        fileInput.value = "";
        return;
      }
      const reader = new FileReader();
      reader.addEventListener("load", () => {
        imageValue.value = String(reader.result || "");
        syncPreview();
      });
      reader.readAsDataURL(file);
    });
    removeButton.addEventListener("click", () => {
      imageValue.value = "";
      fileInput.value = "";
      syncPreview();
    });
    syncPreview();
  }

  async function save(event, product) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!Design.ProductValidators.validate(form)) return;
    U.setSubmitting(form, true);
    try {
      const payload = Design.ProductMappers.toPayload(form);
      const saved = product
        ? await Design.Api.products.update(product.id, payload)
        : await Design.Api.products.create(payload);
      const id = saved?.produitId || saved?.ProduitId || product?.id;
      await Design.WorkspacePage.finalizeMutation({
        successTitle: product ? "Produit mis à jour" : "Produit créé",
        successMessage: "Les données ont été enregistrées dans l’API.",
        onRefreshed: () => Design.Router.go(id && Store.byId.product(String(id)) ? `product/${id}` : "products"),
        onRefreshFailed: () => Design.Router.go(product ? `product/${product.id}` : "products"),
      });
    } catch (error) {
      Design.Components.toast("Enregistrement impossible", error.message, "error");
    } finally {
      U.setSubmitting(form, false);
    }
  }

  Design.ProductForms = { render };
})();
