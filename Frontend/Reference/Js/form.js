(function () {
  const Design = window.SopmineDesign;

  function api(kind) {
    return kind === "family" ? Design.Api.families : Design.Api.units;
  }

  function noun(kind) {
    return kind === "family" ? "famille" : "unité";
  }

  async function create(kind) {
    const value = await editValue({
      title: `Nouvelle ${noun(kind)}`,
      label: kind === "family" ? "Nom de la famille" : "Nom de l\u2019unit\u00e9",
    });
    if (!value) return;
    await mutate(() => api(kind).create(value), "Référentiel ajouté");
  }

  async function update(kind, item) {
    const value = await editValue({
      title: kind === "family" ? "Modifier la famille" : "Modifier l\u2019unit\u00e9",
      label: kind === "family" ? "Nom de la famille" : "Nom de l\u2019unit\u00e9",
      value: item.name,
    });
    if (!value) return;
    await mutate(() => api(kind).update(item.id, value), "Référentiel modifié");
  }

  function editValue({ title, label, value = "" }) {
    return new Promise((resolve) => {
      const dialog = Design.ReferenceDom.formDialog();
      const form = dialog.querySelector("[data-reference-form]");
      const input = form.elements.referenceName;
      Design.Dom.setText(dialog, "[data-reference-form-title]", title);
      Design.Dom.setText(dialog, "[data-reference-form-label]", label);
      input.value = value;

      dialog.querySelector("[data-reference-cancel]").addEventListener("click", () => dialog.close("cancel"));
      form.addEventListener("submit", (event) => {
        event.preventDefault();
        if (!form.reportValidity()) return;
        dialog.close("submit");
      });
      dialog.addEventListener("cancel", (event) => {
        event.preventDefault();
        dialog.close("cancel");
      });
      dialog.addEventListener("close", () => {
        const result = dialog.returnValue === "submit" ? input.value.trim() : null;
        dialog.remove();
        resolve(result || null);
      }, { once: true });

      document.body.appendChild(dialog);
      dialog.showModal();
      input.focus();
      input.select();
    });
  }

  async function remove(kind, item) {
    const confirmed = await Design.Components.confirmDelete({
      title: `Supprimer cette ${noun(kind)} ?`,
      target: item.name,
      message: `Cette ${noun(kind)} sera retirée du référentiel Sopmine.`,
    });
    if (!confirmed) return;
    await mutate(() => api(kind).remove(item.id), "Référentiel supprimé");
  }

  async function mutate(action, successTitle) {
    try {
      await action();
      await Design.WorkspacePage.finalizeMutation({
        successTitle,
        successMessage: "La modification a été enregistrée dans l’API.",
        onRefreshed: () => Design.ReferenceList.show(),
        onRefreshFailed: () => Design.ReferenceList.show(),
      });
    } catch (error) {
      Design.Components.toast("Opération impossible", error.message, "error");
    }
  }

  Design.ReferenceForms = { create, update, remove };
})();
