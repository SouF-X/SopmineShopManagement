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
    const values = product || { name: "", reference: "", imageUrl: "", quantity: 0, minimum: 5, purchase: 0, purchaseTtc: 0, vat: 20, margin: 0, saleHt: 0, sale: 0 };
    set(form, "name", values.name);
    set(form, "reference", values.reference);
    set(form, "imageUrl", values.imageUrl);
    set(form, "quantity", values.quantity);
    set(form, "minimum", values.minimum);
    set(form, "purchase", values.purchase);
    set(form, "purchaseTtc", values.purchaseTtc);
    set(form, "vat", values.vat);
    set(form, "margin", values.margin);
    set(form, "saleHt", values.saleHt);
    set(form, "saleTtc", values.sale);
    setupImageUpload(form);

    let lastEditedField = "margin";
    const updatePricing = (editedField, preserveEditedValue = false) => {
      const editedValue = form.elements[editedField]?.value;
      const pricing = Design.ProductMappers.calculatePricing({
        purchase: U.number(form.elements.purchase.value),
        purchaseTtc: U.number(form.elements.purchaseTtc.value),
        saleHt: U.number(form.elements.saleHt.value),
        saleTtc: U.number(form.elements.saleTtc.value),
        vat: U.number(form.elements.vat.value),
        margin: U.number(form.elements.margin.value),
      }, editedField);

      ["purchase", "purchaseTtc", "saleHt", "saleTtc", "margin"].forEach((field) => {
        if (!preserveEditedValue || field !== editedField) form.elements[field].value = pricing[field];
      });
      if (preserveEditedValue && form.elements[editedField]) form.elements[editedField].value = editedValue;
      Design.Dom.setText(page, "[data-sale-ht]", U.money(pricing.saleHt));
      Design.Dom.setText(page, "[data-sale-ttc]", U.money(pricing.saleTtc));
      Design.Dom.setText(page, "[data-summary-purchase]", U.money(pricing.purchase));
      Design.Dom.setText(page, "[data-summary-sale]", U.money(pricing.saleHt));
      Design.Dom.setText(page, "[data-summary-vat]", `${U.number(form.elements.vat.value)} %`);
      Design.Dom.setText(page, "[data-summary-margin]", `${pricing.margin.toFixed(1)} %`);
    };

    ["purchase", "purchaseTtc", "saleHt", "saleTtc", "vat", "margin"].forEach((field) => {
      form.elements[field].addEventListener("input", () => {
        lastEditedField = field;
        updatePricing(lastEditedField, true);
      });
      form.elements[field].addEventListener("blur", () => updatePricing(field));
    });
    updatePricing(lastEditedField);
    form.addEventListener("submit", (event) => save(event, product));
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
