(function () {
  const Design = window.SopmineDesign;

  function setBusy(button, busy, label) {
    button.disabled = busy;
    button.setAttribute("aria-busy", String(busy));
    if (label) button.querySelector("span:last-child").textContent = busy ? "Enregistrement…" : label;
  }

  function setPasswordVisibility(button, visible) {
    const control = button.closest("[data-password-control]");
    const input = control?.querySelector("[data-password-input]");
    const icon = button.querySelector(".material-symbols-rounded");
    if (!input) return;

    input.type = visible ? "text" : "password";
    button.setAttribute("aria-pressed", String(visible));
    button.setAttribute("aria-label", visible ? "Masquer le mot de passe" : "Afficher le mot de passe");
    button.setAttribute("title", visible ? "Masquer le mot de passe" : "Afficher le mot de passe");
    if (icon) icon.textContent = visible ? "visibility_off" : "visibility";
  }

  function bindPasswordVisibilityToggles(root) {
    root.querySelectorAll("[data-password-toggle]").forEach((button) => {
      setPasswordVisibility(button, false);
      button.addEventListener("click", () => {
        const input = button.closest("[data-password-control]")?.querySelector("[data-password-input]");
        setPasswordVisibility(button, input?.type === "password");
      });
    });
  }

  function resetPasswordVisibility(root) {
    root.querySelectorAll("[data-password-toggle]").forEach((button) => setPasswordVisibility(button, false));
  }
  function openUser(user = null) {
    const dialog = document.querySelector("#settings-user-dialog");
    const form = dialog.querySelector("[data-user-form]");
    const passwordField = dialog.querySelector("[data-user-password-field]");
    const password = form.elements.password;
    const submit = dialog.querySelector("[data-user-submit]");
    const submitLabel = user ? "Enregistrer" : "Créer le compte";

    form.reset();
    resetPasswordVisibility(dialog);
    form.elements.email.value = user?.email || "";
    form.elements.role.value = user?.role || "Employee";
    passwordField.hidden = Boolean(user);
    password.required = !user;
    Design.Dom.setText(dialog, "[data-user-dialog-title]", user ? "Modifier l’utilisateur" : "Nouvel utilisateur");
    Design.Dom.setText(dialog, "[data-user-submit-label]", submitLabel);
    dialog.querySelector("[data-user-cancel]").onclick = () => dialog.close();
    form.onsubmit = async (event) => {
      event.preventDefault();
      if (!form.reportValidity()) return;
      setBusy(submit, true, submitLabel);
      try {
        const payload = { email: form.elements.email.value.trim(), role: form.elements.role.value };
        if (user) await Design.SettingsPage.updateUser(user.userId, payload);
        else await Design.SettingsPage.createUser({ ...payload, password: password.value });
        dialog.close();
        Design.Components.toast(user ? "Utilisateur modifié" : "Utilisateur créé", "Les accès ont été enregistrés dans l’API.");
        Design.SettingsPage.rerender();
      } catch (error) {
        Design.Components.toast("Opération impossible", error.message, "error");
      } finally {
        setBusy(submit, false, submitLabel);
      }
    };
    dialog.showModal();
    requestAnimationFrame(() => form.elements.email.focus());
  }

  function openPassword(user) {
    const dialog = document.querySelector("#settings-password-dialog");
    const form = dialog.querySelector("[data-password-form]");
    const submit = dialog.querySelector("[data-password-submit]");
    form.reset();
    resetPasswordVisibility(dialog);
    Design.Dom.setText(dialog, "[data-password-user]", user.email);
    dialog.querySelector("[data-password-cancel]").onclick = () => dialog.close();
    form.onsubmit = async (event) => {
      event.preventDefault();
      if (!form.reportValidity()) return;
      submit.disabled = true;
      submit.setAttribute("aria-busy", "true");
      try {
        await Design.SettingsPage.resetPassword(user.userId, form.elements.newPassword.value);
        dialog.close();
        Design.Components.toast("Mot de passe réinitialisé", `Un nouveau mot de passe a été défini pour ${user.email}.`);
      } catch (error) {
        Design.Components.toast("Réinitialisation impossible", error.message, "error");
      } finally {
        submit.disabled = false;
        submit.setAttribute("aria-busy", "false");
      }
    };
    dialog.showModal();
    requestAnimationFrame(() => form.elements.newPassword.focus());
  }

  function openCurrentPassword() {
    const dialog = document.querySelector("#settings-current-password-dialog");
    const form = dialog.querySelector("[data-current-password-form]");
    const submit = dialog.querySelector("[data-current-password-submit]");
    form.reset();
    resetPasswordVisibility(dialog);
    dialog.querySelector("[data-current-password-cancel]").onclick = () => dialog.close();
    form.onsubmit = async (event) => {
      event.preventDefault();
      if (!form.reportValidity()) return;
      setBusy(submit, true, "Mettre à jour");
      try {
        await Design.Api.settings.users.changeCurrentPassword(form.elements.currentPassword.value, form.elements.newPassword.value);
        dialog.close();
        Design.Components.toast("Mot de passe mis à jour", "Votre nouveau mot de passe est enregistré.");
      } catch (error) {
        Design.Components.toast("Mise à jour impossible", error.message, "error");
      } finally {
        setBusy(submit, false, "Mettre à jour");
      }
    };
    dialog.showModal();
    requestAnimationFrame(() => form.elements.currentPassword.focus());
  }

  async function removeUser(user) {
    const confirmed = await Design.Components.confirmDelete({
      title: "Supprimer l’utilisateur ?",
      target: user.email,
      message: "Ce compte ne pourra plus accéder à Sopmine.",
    });
    if (!confirmed) return;
    try {
      await Design.SettingsPage.removeUser(user.userId);
      Design.Components.toast("Utilisateur supprimé", "Le compte a été retiré de l’API.");
      Design.SettingsPage.rerender();
    } catch (error) {
      Design.Components.toast("Suppression impossible", error.message, "error");
    }
  }

  async function saveNomination(setting, form) {
    if (!form.reportValidity()) return;
    const submit = form.querySelector("[data-nomination-save]");
    submit.disabled = true;
    submit.setAttribute("aria-busy", "true");
    try {
      await Design.SettingsPage.updateNomination(setting.key, {
        root: form.elements.root.value.trim(),
        dateFormat: form.elements.dateFormat.value,
        incrementSize: Number(form.elements.incrementSize.value),
      });
      Design.Components.toast("Format enregistré", `${setting.label} utilisera la nouvelle numérotation.`);
      Design.SettingsPage.rerender();
    } catch (error) {
      Design.Components.toast("Enregistrement impossible", error.message, "error");
    } finally {
      submit.disabled = false;
      submit.setAttribute("aria-busy", "false");
    }
  }

  bindPasswordVisibilityToggles(document);
  Design.SettingsForms = { openUser, openPassword, openCurrentPassword, removeUser, saveNomination };
})();
