(function () {
  const Design = window.SopmineDesign;

  function role(value) {
    return Number(value) === 1 || String(value).toLowerCase() === "sav" ? "SAV" : "Commercial";
  }

  function mapSupplier(dto) {
    const contacts = (dto.contacts || dto.Contacts || []).map((contact) => ({
      id: String(contact.contactFournisseurId || contact.ContactFournisseurId || ""),
      name: String(contact.nom || contact.Nom || "Contact"),
      phone: String(contact.tel || contact.Tel || ""),
      role: role(contact.role ?? contact.Role),
    }));
    return {
      id: String(dto.fournisseurId || dto.FournisseurId || ""),
      createdAt: dto.createdAtUtc || dto.CreatedAtUtc || null,
      name: String(dto.nom || dto.Nom || "Fournisseur sans nom"),
      ice: String(dto.ice || dto.ICE || "—"),
      address: String(dto.adresse || dto.Adresse || ""),
      city: String(dto.ville || dto.Ville || "Non renseignée"),
      phone: String(dto.telFix || dto.TelFix || ""),
      website: String(dto.siteWeb || dto.SiteWeb || ""),
      email: String(dto.email || dto.Email || ""),
      contacts,
    };
  }

  function contactsFromForm(form) {
    return [...form.querySelectorAll("[data-contact-entry]")]
      .map((entry) => ({
        contactFournisseurId: Design.Utils.optional(entry.dataset.contactId),
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
      ice: String(values.get("ice") || "").trim(),
      adresse: String(values.get("address") || "").trim(),
      ville: String(values.get("city") || "").trim(),
      telFix: String(values.get("phone") || "").trim(),
      siteWeb: Design.Utils.optional(values.get("website")),
      email: Design.Utils.optional(values.get("email")),
      contacts: contactsFromForm(form),
    };
  }

  Design.SupplierMappers = { mapSupplier, toPayload };
})();
