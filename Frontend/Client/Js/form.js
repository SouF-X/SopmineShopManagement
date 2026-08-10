(function () {
  const Design = window.SopmineDesign;

  function render(id) {
    const client = id ? Design.Store.byId.client(id) : null;
    if (id && !client) return Design.Shell.missing("Ce client n’existe plus", "clients");
    const page = Design.ClientDom.form();
    const form = page.querySelector("[data-client-form]");
    const back = client ? `client/${id}` : "clients";
    page.querySelectorAll("[data-back]").forEach((button) => button.dataset.route = back);
    Design.Dom.setText(page, "[data-form-title]", client ? "Modifier un client" : "Créer un client");
    const value = client || { name: "", typeValue: 1, ice: "", city: "", phone: "", address: "", contacts: [] };
    form.elements.name.value = value.name;
    form.elements.type.value = String(value.typeValue);
    form.elements.ice.value = value.ice === "—" ? "" : value.ice;
    form.elements.city.value = value.city;
    form.elements.phone.value = value.phone;
    form.elements.address.value = value.address;
    const contacts = value.contacts.length ? value.contacts : [{ id: "", name: "", phone: "", role: "Commercial" }];
    page.querySelector("[data-contact-list]").replaceChildren(...contacts.map(contactEntry));
    page.querySelector("[data-add-contact]").addEventListener("click", () => page.querySelector("[data-contact-list]").appendChild(contactEntry()));
    form.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-contact]");
      if (!button) return;
      const entries = form.querySelectorAll("[data-contact-entry]");
      if (entries.length > 1) button.closest("[data-contact-entry]").remove();
    });
    form.addEventListener("submit", (event) => save(event, client));
    Design.Shell.mount(page, "clients", client ? "Modifier" : "Ajouter");
  }

  function contactEntry(contact = {}) {
    const entry = Design.ClientDom.contact();
    entry.dataset.contactId = contact.id || "";
    entry.querySelector("[data-contact-name]").value = contact.name || "";
    entry.querySelector("[data-contact-phone]").value = contact.phone || "";
    entry.querySelector("[data-contact-role]").value = contact.role || "Commercial";
    return entry;
  }

  async function save(event, client) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!Design.ClientValidators.validate(form)) return;
    Design.Utils.setSubmitting(form, true);
    try {
      const payload = Design.ClientMappers.toPayload(form);
      const saved = client
        ? await Design.Api.clients.update(client.id, payload)
        : await Design.Api.clients.create(payload);
      const id = String(saved?.clientId || saved?.ClientId || client?.id || "");
      await Design.WorkspacePage.finalizeMutation({
        successTitle: client ? "Client mis à jour" : "Client créé",
        successMessage: "La fiche a été enregistrée dans l’API.",
        onRefreshed: () => Design.Router.go(id && Design.Store.byId.client(id) ? `client/${id}` : "clients"),
        onRefreshFailed: () => Design.Router.go(client ? `client/${client.id}` : "clients"),
      });
    } catch (error) {
      Design.Components.toast("Enregistrement impossible", error.message, "error");
    } finally {
      Design.Utils.setSubmitting(form, false);
    }
  }

  Design.ClientForms = { render };
})();
