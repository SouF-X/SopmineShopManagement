(function () {
  const Design = window.SopmineDesign;

  function render(id) {
    const supplier = id ? Design.Store.byId.supplier(id) : null;
    if (id && !supplier) return Design.Shell.missing("Ce fournisseur n’existe plus", "suppliers");
    const page = Design.SupplierDom.form();
    const form = page.querySelector("[data-supplier-form]");
    const back = supplier ? `supplier/${id}` : "suppliers";
    page.querySelectorAll("[data-back]").forEach((button) => button.dataset.route = back);
    Design.Dom.setText(page, "[data-form-title]", supplier ? "Modifier un fournisseur" : "Créer un fournisseur");
    const value = supplier || { name: "", ice: "", city: "", phone: "", address: "", email: "", website: "", contacts: [] };
    form.elements.name.value = value.name;
    form.elements.ice.value = value.ice === "—" ? "" : value.ice;
    form.elements.city.value = value.city;
    form.elements.phone.value = value.phone;
    form.elements.address.value = value.address;
    form.elements.email.value = value.email;
    form.elements.website.value = value.website;
    const contacts = value.contacts.length ? value.contacts : [{ id: "", name: "", phone: "", role: "Commercial" }];
    page.querySelector("[data-contact-list]").replaceChildren(...contacts.map(contactEntry));
    page.querySelector("[data-add-contact]").addEventListener("click", () => page.querySelector("[data-contact-list]").appendChild(contactEntry()));
    form.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-contact]");
      if (!button) return;
      const entries = form.querySelectorAll("[data-contact-entry]");
      if (entries.length > 1) button.closest("[data-contact-entry]").remove();
    });
    form.addEventListener("submit", (event) => save(event, supplier));
    Design.Shell.mount(page, "suppliers", supplier ? "Modifier" : "Ajouter");
  }

  function contactEntry(contact = {}) {
    const entry = Design.SupplierDom.contact();
    entry.dataset.contactId = contact.id || "";
    entry.querySelector("[data-contact-name]").value = contact.name || "";
    entry.querySelector("[data-contact-phone]").value = contact.phone || "";
    entry.querySelector("[data-contact-role]").value = contact.role || "Commercial";
    return entry;
  }

  async function save(event, supplier) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!Design.SupplierValidators.validate(form)) return;
    Design.Utils.setSubmitting(form, true);
    try {
      const payload = Design.SupplierMappers.toPayload(form);
      const saved = supplier
        ? await Design.Api.suppliers.update(supplier.id, payload)
        : await Design.Api.suppliers.create(payload);
      const id = String(saved?.fournisseurId || saved?.FournisseurId || supplier?.id || "");
      await Design.WorkspacePage.finalizeMutation({
        successTitle: supplier ? "Fournisseur mis à jour" : "Fournisseur créé",
        successMessage: "La fiche a été enregistrée dans l’API.",
        onRefreshed: () => Design.Router.go(id && Design.Store.byId.supplier(id) ? `supplier/${id}` : "suppliers"),
        onRefreshFailed: () => Design.Router.go(supplier ? `supplier/${supplier.id}` : "suppliers"),
      });
    } catch (error) {
      Design.Components.toast("Enregistrement impossible", error.message, "error");
    } finally {
      Design.Utils.setSubmitting(form, false);
    }
  }

  Design.SupplierForms = { render };
})();
