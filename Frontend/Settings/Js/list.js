(function () {
  const Design = window.SopmineDesign;

  function show(tab) {
    const page = Design.SettingsDom.page();
    Design.Components.pageHeader(page.querySelector("[data-page-header]"), {
      eyebrow: "Administration du point de vente",
      icon: "settings",
      title: "Paramètres",
      description: "Contrôlez les accès et les références générées par vos documents commerciaux.",
    });
    page.querySelectorAll("[data-settings-tab]").forEach((button) => {
      const active = button.dataset.settingsTab === tab;
      button.classList.toggle("is-active", active);
      button.setAttribute("aria-current", active ? "page" : "false");
      button.onclick = () => Design.Router.go(`settings/${button.dataset.settingsTab}`);
    });
    page.querySelector("[data-settings-content]").replaceChildren(tab === "numbering" ? numbering() : users());
    Design.Shell.mount(page, "settings", tab === "numbering" ? "Numérotation" : "Utilisateurs");
  }

  function users() {
    const panel = Design.SettingsDom.users();
    const items = Design.SettingsPage.state.users;
    const currentEmail = window.SopmineAuth?.getSessionEmail?.(window.SopmineAuth?.getSession?.())?.toLowerCase() || "";
    Design.Dom.setText(panel, "[data-user-count]", items.length);
    Design.Dom.setText(panel, "[data-admin-count]", items.filter((item) => item.role.toLowerCase() === "admin").length);
    Design.Dom.setText(panel, "[data-employee-count]", items.filter((item) => item.role.toLowerCase() !== "admin").length);
    panel.querySelector("[data-create-user]").onclick = () => Design.SettingsForms.openUser();
    panel.querySelector("[data-current-password]").onclick = () => Design.SettingsForms.openCurrentPassword();
    const list = panel.querySelector("[data-user-list]");
    if (!items.length) {
      list.appendChild(Design.SettingsDom.usersEmpty());
      return panel;
    }
    list.replaceChildren(...items.map((user) => userRow(user, currentEmail)));
    return panel;
  }

  function userRow(user, currentEmail) {
    const row = Design.SettingsDom.userRow();
    const isCurrent = user.email.toLowerCase() === currentEmail;
    Design.Dom.setText(row, "[data-user-initials]", Design.Utils.initials(user.email.split("@")[0]));
    Design.Dom.setText(row, "[data-user-email]", user.email);
    Design.Dom.setText(row, "[data-user-current]", isCurrent ? "Compte actuellement connecté" : "Compte Sopmine");
    const role = row.querySelector("[data-user-role]");
    role.className = `status ${user.role.toLowerCase() === "admin" ? "success" : ""}`.trim();
    role.textContent = user.role.toLowerCase() === "admin" ? "Administrateur" : "Employé";
    row.querySelector("[data-user-edit]").addEventListener("click", () => Design.SettingsForms.openUser(user));
    row.querySelector("[data-user-password]").addEventListener("click", () => Design.SettingsForms.openPassword(user));
    const remove = row.querySelector("[data-user-delete]");
    remove.disabled = isCurrent;
    remove.title = isCurrent ? "Le compte connecté ne peut pas être supprimé" : "Supprimer";
    remove.addEventListener("click", () => Design.SettingsForms.removeUser(user));
    row.querySelectorAll("button").forEach((button) => {
      const action = button.hasAttribute("data-user-edit") ? "Modifier" : button.hasAttribute("data-user-password") ? "Réinitialiser le mot de passe de" : "Supprimer";
      button.setAttribute("aria-label", `${action} ${user.email}`);
    });
    return row;
  }

  function numbering() {
    const panel = Design.SettingsDom.numbering();
    const state = Design.SettingsPage.state;
    const settings = state.nominations;
    const list = panel.querySelector("[data-nomination-list]");
    list.replaceChildren();
    if (!settings.length) {
      list.appendChild(Design.Components.emptyState("tag", "Aucun format", "L’API n’a retourné aucun type de document."));
      panel.querySelector("[data-nomination-form]").hidden = true;
      return panel;
    }
    const active = settings.find((item) => item.key === state.activeNominationKey) || settings[0];
    state.activeNominationKey = active.key;
    [
      { nature: 0, label: "Achats", icon: "shopping_cart" },
      { nature: 1, label: "Ventes", icon: "point_of_sale" },
    ].forEach((group) => {
      const groupItems = settings.filter((item) => item.nature === group.nature);
      if (!groupItems.length) return;
      const section = Design.SettingsDom.nominationGroup();
      Design.Dom.setText(section, "[data-group-icon]", group.icon);
      Design.Dom.setText(section, "[data-group-label]", group.label);
      Design.Dom.setText(section, "[data-group-count]", groupItems.length);
      section.querySelector("[data-nomination-group-items]").replaceChildren(
        ...groupItems.map((item) => nominationItem(item, item.key === active.key)));
      list.appendChild(section);
    });

    const form = panel.querySelector("[data-nomination-form]");
    Design.Dom.setText(form, "[data-nomination-icon]", active.icon);
    Design.Dom.setText(form, "[data-nomination-title]", active.label);
    form.elements.root.value = active.root;
    form.elements.dateFormat.value = active.dateFormat;
    form.elements.incrementSize.value = active.incrementSize;
    const preview = form.querySelector("[data-nomination-preview]");
    const updatePreview = () => {
      preview.textContent = Design.SettingsPage.buildNominationPreview({
        root: form.elements.root.value,
        dateFormat: form.elements.dateFormat.value,
        incrementSize: form.elements.incrementSize.value,
      });
    };
    form.oninput = updatePreview;
    form.onchange = updatePreview;
    form.onsubmit = (event) => {
      event.preventDefault();
      Design.SettingsForms.saveNomination(active, form);
    };
    updatePreview();
    return panel;
  }

  function nominationItem(item, active) {
    const button = Design.SettingsDom.nominationItem();
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-pressed", String(active));
    Design.Dom.setText(button, "[data-nomination-item-icon]", item.icon);
    Design.Dom.setText(button, "[data-nomination-item-label]", item.label);
    Design.Dom.setText(button, "[data-nomination-item-root]", `${item.root} · ${item.incrementSize} chiffres`);
    Design.Dom.setText(button, "[data-nomination-item-preview]", Design.SettingsPage.buildNominationPreview(item));
    button.addEventListener("click", () => {
      Design.SettingsPage.setActiveNomination(item.key);
      Design.SettingsPage.rerender();
    });
    return button;
  }

  Design.SettingsList = { show };
})();
