(function () {
  const Design = window.SopmineDesign;

  function showFlashToast() {
    try {
      const raw = window.sessionStorage.getItem("sopmine-flash-toast");
      if (!raw) return;
      window.sessionStorage.removeItem("sopmine-flash-toast");
      const flash = JSON.parse(raw);
      if (flash?.title) {
        Design.Components.toast(flash.title, flash.message || "", flash.type || "success");
      }
    } catch {
      window.sessionStorage.removeItem("sopmine-flash-toast");
    }
  }

  async function start() {
    if (!window.SopmineAuth?.hasValidSession?.()) return;

    try {
      Design.Shell.init();
      showLoading();
      await Design.WorkspacePage.load();
      Design.Router.start();
      showFlashToast();
    } catch (error) {
      showError(error);
    }
  }

  function showLoading() {
    const rootSection = document.body.dataset.defaultRoute?.split("/")[0] || "products";
    const state = Design.Components.apiState({
      icon: "sync",
      eyebrow: "Synchronisation sécurisée",
      title: "Ouverture du point de vente",
      description: "Catalogue sanitaire, partenaires et documents sont synchronisés avec Sopmine.",
    });
    state.classList.add("api-state--loading");
    Design.Shell.mount(state, rootSection);
  }

  function showError(error) {
    const rootSection = document.body.dataset.defaultRoute?.split("/")[0] || "products";
    Design.Shell.setApiStatus("error", "API indisponible");
    const state = Design.Components.apiState({
      icon: "cloud_off",
      eyebrow: "API indisponible",
      title: "Impossible de charger les données",
      description: error?.message || "Vérifiez que l’API est démarrée et que votre session est valide.",
      retry: true,
    });
    state.classList.add("api-state--error");
    state.querySelector("[data-api-retry]").addEventListener("click", async () => {
      showLoading();
      try {
        await Design.WorkspacePage.reload();
        Design.Router.start();
      } catch (retryError) {
        showError(retryError);
      }
    });
    Design.Shell.mount(state, rootSection);
  }

  start();
})();
