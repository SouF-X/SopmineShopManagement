(function () {
  const Design = window.SopmineDesign;
  const state = {
    users: [],
    nominations: [],
    activeTab: "users",
    activeNominationKey: null,
    ready: false,
    loading: false,
  };

  function formatDatePart(format, date) {
    const month = String(date.getMonth() + 1).padStart(2, "0");
    if (format === "none") return "";
    if (format === "yyMM") return `${String(date.getFullYear()).slice(-2)}${month}`;
    if (format === "yyyyMM") return `${date.getFullYear()}${month}`;
    return month;
  }

  function buildNominationPreview(setting, date = new Date(), sequence = 1) {
    const root = String(setting?.root || "DOC").trim() || "DOC";
    const datePart = formatDatePart(setting?.dateFormat || "MM", date);
    const size = Math.min(8, Math.max(1, Number(setting?.incrementSize) || 3));
    const increment = String(sequence).padStart(size, "0");
    return [root, datePart, increment].filter(Boolean).join("-");
  }

  function normalizeUser(user) {
    return {
      userId: String(user?.userId ?? user?.UserId ?? ""),
      email: String(user?.email ?? user?.Email ?? ""),
      role: String(user?.role ?? user?.Role ?? "Employee"),
    };
  }

  function normalizeNomination(item) {
    return {
      key: String(item?.key ?? item?.Key ?? ""),
      nature: Number(item?.nature ?? item?.Nature ?? 1),
      type: Number(item?.type ?? item?.Type ?? 0),
      label: String(item?.label ?? item?.Label ?? "Document"),
      icon: String(item?.icon ?? item?.Icon ?? "receipt_long"),
      root: String(item?.root ?? item?.Root ?? "DOC"),
      dateFormat: String(item?.dateFormat ?? item?.DateFormat ?? "MM"),
      incrementSize: Number(item?.incrementSize ?? item?.IncrementSize ?? 3),
    };
  }

  function sortUsers(users) {
    return users.sort((left, right) => left.email.localeCompare(right.email, "fr", { sensitivity: "base" }));
  }

  async function load() {
    const [users, nominations] = await Promise.all([
      Design.Api.settings.users.list(),
      Design.Api.settings.nominations.list(),
    ]);
    state.users = sortUsers((Array.isArray(users) ? users : []).map(normalizeUser));
    state.nominations = Array.from(new Map((Array.isArray(nominations) ? nominations : [])
      .map(normalizeNomination)
      .map((item) => [item.key, item])).values());
    if (!state.nominations.some((item) => item.key === state.activeNominationKey)) {
      state.activeNominationKey = state.nominations[0]?.key || null;
    }
    state.ready = true;
  }

  function loadingState() {
    const node = Design.Components.apiState({
      icon: "settings_suggest",
      eyebrow: "Configuration sécurisée",
      title: "Chargement des paramètres",
      description: "Les utilisateurs et les formats de documents sont synchronisés avec l’API.",
    });
    node.classList.add("api-state--loading");
    Design.Shell.mount(node, "settings", state.activeTab === "numbering" ? "Numérotation" : "Utilisateurs");
  }

  function errorState(error) {
    const node = Design.Components.apiState({
      icon: "cloud_off",
      eyebrow: "Paramètres indisponibles",
      title: "Impossible de charger la configuration",
      description: error?.message || "Vérifiez la connexion à l’API.",
      retry: true,
    });
    node.classList.add("api-state--error");
    node.querySelector("[data-api-retry]").addEventListener("click", () => {
      state.ready = false;
      render(state.activeTab);
    });
    Design.Shell.mount(node, "settings", "Erreur");
  }

  function isCurrentRoute() {
    return Design.Router?.current?.() === `settings/${state.activeTab}`;
  }

  async function render(tab = "users") {
    state.activeTab = tab === "numbering" ? "numbering" : "users";
    if (state.ready) return Design.SettingsList.show(state.activeTab);
    if (state.loading) return;
    state.loading = true;
    loadingState();
    try {
      await load();
      if (isCurrentRoute()) Design.SettingsList.show(state.activeTab);
    } catch (error) {
      if (isCurrentRoute()) errorState(error);
    } finally {
      state.loading = false;
    }
  }

  function rerender() {
    Design.SettingsList.show(state.activeTab);
  }

  function setActiveNomination(key) {
    state.activeNominationKey = key;
  }

  async function createUser(payload) {
    state.users.push(normalizeUser(await Design.Api.settings.users.create(payload)));
    sortUsers(state.users);
  }

  async function updateUser(userId, payload) {
    const updated = normalizeUser(await Design.Api.settings.users.update(userId, payload));
    state.users = sortUsers(state.users.map((item) => item.userId === userId ? updated : item));
  }

  async function resetPassword(userId, newPassword) {
    await Design.Api.settings.users.resetPassword(userId, newPassword);
  }

  async function removeUser(userId) {
    await Design.Api.settings.users.remove(userId);
    state.users = state.users.filter((item) => item.userId !== userId);
  }

  async function updateNomination(key, payload) {
    const updated = normalizeNomination(await Design.Api.settings.nominations.update(key, payload));
    state.nominations = state.nominations.map((item) => item.key === key ? updated : item);
    state.activeNominationKey = updated.key;
  }

  Design.SettingsPage = {
    render,
    rerender,
    state,
    setActiveNomination,
    createUser,
    updateUser,
    resetPassword,
    removeUser,
    updateNomination,
    buildNominationPreview,
  };
})();
