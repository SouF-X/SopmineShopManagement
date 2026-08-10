(function () {
  const Design = window.SopmineDesign;

  function role(value) {
    return Number(value) === 1 || String(value).toLowerCase() === "sav" ? "SAV" : "Commercial";
  }

  function mapClient(dto) {
    const rawType = dto.type ?? dto.Type ?? 0;
    const numericType = Number(rawType);
    const typeValue = Number.isFinite(numericType)
      ? numericType
      : String(rawType).toLowerCase() === "professionnel" ? 1 : 0;
    const contacts = (dto.contacts || dto.Contacts || []).map((contact) => ({
      id: String(contact.contactClientId || contact.ContactClientId || ""),
      name: String(contact.nom || contact.Nom || "Contact"),
      phone: String(contact.tel || contact.Tel || ""),
      role: role(contact.role ?? contact.Role),
    }));
    const primary = contacts[0];
    return {
      id: String(dto.clientId || dto.ClientId || ""),
      name: String(dto.nom || dto.Nom || "Client sans nom"),
      typeValue,
      type: typeValue === 1 ? "Professionnel" : "Particulier",
      ice: String(dto.ice || dto.ICE || "—"),
      address: String(dto.adresse || dto.Adresse || ""),
      city: String(dto.ville || dto.Ville || "Non renseignée"),
      phone: String(dto.tel || dto.Tel || ""),
      contacts,
      contact: primary?.name || "Aucun contact",
      role: primary?.role || "—",
    };
  }

  function contactsFromForm(form) {
    return [...form.querySelectorAll("[data-contact-entry]")]
      .map((entry) => ({
        contactClientId: Design.Utils.optional(entry.dataset.contactId),
        nom: entry.querySelector("[data-contact-name]").value.trim(),
        tel: entry.querySelector("[data-contact-phone]").value.trim(),
        role: entry.querySelector("[data-contact-role]").value === "SAV" ? 1 : 0,
      }))
      .filter((contact) => contact.nom || contact.tel);
  }

  function toPayload(form) {
    const values = new FormData(form);
    return {
      nom: String(values.get("name") || "").trim(),
      type: Number(values.get("type") || 0),
      ice: Design.Utils.optional(values.get("ice")),
      adresse: Design.Utils.optional(values.get("address")),
      ville: Design.Utils.optional(values.get("city")),
      tel: String(values.get("phone") || "").trim(),
      contacts: contactsFromForm(form),
    };
  }

  Design.ClientMappers = { mapClient, toPayload };
})();
