(function () {
  const Design = window.SopmineDesign;
  const workspace = document.querySelector("#workspace");
  const appStage = document.querySelector(".app-stage");
  const sidebar = document.querySelector("#sidebar");
  const scrim = document.querySelector("#mobile-scrim");
  const mobileMenu = document.querySelector("#mobile-menu");
  const command = document.querySelector("#command-layer");
  const commandTrigger = document.querySelector("#command-trigger");
  const commandInput = document.querySelector("#command-input");
  const profileMenu = document.querySelector("#profile-menu");
  const profileTriggers = Array.from(document.querySelectorAll(".profile-menu-trigger"));
  const wideNavigation = window.matchMedia("(min-width: 1200px)");
  const tabletNavigation = window.matchMedia("(min-width: 768px) and (max-width: 1199px)");
  let activeProfileTrigger = null;
  let activeMobileMenu = null;
  let activeMobileCommerceMode = null;

  function ensureMobileBrandContext() {
    const topbarStart = document.querySelector(".topbar-start");
    if (!topbarStart || topbarStart.querySelector("#mobile-brand-context")) return;
    const brand = document.createElement("div");
    brand.className = "mobile-brand-context";
    brand.id = "mobile-brand-context";
    brand.innerHTML = `<img src="/shared/assets/sopmine-logo.jpeg" alt="Sopmine" width="34" height="34" /><span><strong>Sopmine</strong><small data-mobile-context>Sopmine / Espace</small></span>`;
    topbarStart.prepend(brand);
  }

  function ensureResponsiveTopbarActions() {
    const topbarEnd = document.querySelector(".topbar-end");
    if (!topbarEnd || topbarEnd.querySelector(".topbar-utility-actions")) return;
    const controls = [
      topbarEnd.querySelector("#command-trigger"),
      topbarEnd.querySelector("#theme-toggle"),
      topbarEnd.querySelector(".notification-button")
    ].filter(Boolean);
    const profile = topbarEnd.querySelector("#topbar-profile");
    if (!controls.length && !profile) return;

    const actions = document.createElement("div");
    actions.className = "topbar-utility-actions";
    actions.id = "topbar-utility-actions";
    controls.forEach((control) => actions.append(control));

    const trigger = document.createElement("button");
    trigger.className = "icon-button topbar-more";
    trigger.id = "topbar-more";
    trigger.type = "button";
    trigger.setAttribute("aria-label", "Ouvrir les options rapides");
    trigger.setAttribute("aria-controls", "topbar-utility-actions");
    trigger.setAttribute("aria-expanded", "false");
    trigger.innerHTML = '<span class="material-symbols-rounded">tune</span>';

    topbarEnd.append(actions);
    if (profile) topbarEnd.append(profile);
    topbarEnd.append(trigger);
  }
  function init() {
    ensureMobileBrandContext();
    ensureResponsiveTopbarActions();
    Design.Dom.hideDecorativeIcons(document);
    Design.Controls?.start(workspace);
    bindNavigation();
    bindShellActions();
    syncProfile();
    const savedTheme = localStorage.getItem("sopmine-design-theme");
    if (["light", "dark"].includes(savedTheme)) document.documentElement.dataset.theme = savedTheme;
    if (wideNavigation.matches && localStorage.getItem("sopmine-sidebar-collapsed") === "true") {
      document.body.classList.add("sidebar-collapsed");
    }
    updateThemeIcon();
    updateSidebarToggle();
  }

  function mount(content, section, action = "", feature = "") {
    closeSidebar();
    closeMobileSheet();
    closeCommand();
    closeProfileMenu();
    Design.Controls?.close();
    const previousView = workspace.firstElementChild;
    if (previousView?.matches?.("[data-static-view]")) {
      previousView.hidden = true;
      document.querySelector("#design-template-host")?.appendChild(previousView);
    }
    content.hidden = false;
    workspace.replaceChildren(content);
    workspace.dataset.section = section;
    workspace.focus({ preventScroll: true });
    updateNavigation(section);
    updateBreadcrumbs(section, feature, action);
    Design.Controls?.refresh(workspace);
    appStage?.scrollTo({ top: 0, behavior: "auto" });
  }

  function missing(title, returnRoute) {
    const state = Design.Components.apiState({
      icon: "find_in_page",
      eyebrow: "Élément introuvable",
      title,
      description: "L’élément a peut-être été supprimé ou n’est plus accessible avec votre rôle.",
    });
    state.classList.add("api-state--error");
    const button = Design.Dom.clone("missing-action-template");
    button.dataset.route = returnRoute;
    state.appendChild(button);
    mount(state, rootSection(returnRoute));
  }

  function setApiStatus(status, label) {
    const node = document.querySelector("#api-status");
    if (!node) return;
    node.dataset.status = status;
    node.querySelector("span:last-child").textContent = label;
  }

  function updateCounts() {
    const counts = {
      products: Design.Store.state.products.length,
      suppliers: Design.Store.state.suppliers.length,
      clients: Design.Store.state.clients.length,
      purchases: Design.Store.state.purchases.length,
      sales: Design.Store.state.sales.length,
    };
    Object.entries(counts).forEach(([route, total]) => {
      const badge = document.querySelector(`[data-route="${route}"] .nav-count`)
        || document.querySelector(`[data-nav-root="${route}"] .nav-count`);
      if (badge) badge.textContent = total;
    });
  }

  function syncProfile() {
    const session = window.SopmineAuth?.getSession?.();
    const email = window.SopmineAuth?.getSessionEmail?.(session) || "Utilisateur";
    const role = window.SopmineAuth?.getSessionRole?.(session) || "Compte Sopmine";
    const name = email.split("@")[0].replace(/[._-]+/g, " ") || "Utilisateur";
    const initials = Design.Utils.initials(name);
    document.querySelector(".workspace-chip strong").textContent = name;
    document.querySelector(".workspace-chip small").textContent = role;
    document.querySelectorAll(".workspace-icon, .topbar-avatar, .profile-menu-avatar").forEach((node) => { node.textContent = initials; });
    document.querySelector(".profile-menu-name").textContent = name;
    document.querySelector(".profile-menu-role").textContent = role;
    document.querySelector('[data-nav-family="purchases"]').hidden = role.toLowerCase() === "employee";
    document.querySelectorAll("[data-mobile-purchases]").forEach((node) => { node.hidden = role.toLowerCase() === "employee"; });
    const isEmployee = window.SopmineAuth?.isEmployeeSession?.(session) ?? false;
    const isAdmin = !isEmployee && role.toLowerCase() === "admin";
    document.querySelectorAll("[data-admin-only]").forEach((node) => { node.hidden = !isAdmin; });
  }

  function updateNavigation(section) {
    const route = Design.Router.current();
    const isCommerce = section === "purchases" || section === "sales";
    const isPartners = section === "suppliers" || section === "clients";
    const isMore = section === "references" || section === "settings";
    document.querySelectorAll(".nav-item").forEach((item) => {
      const active = item.dataset.navRoot === section || item.dataset.route === section;
      item.classList.toggle("is-active", active);

    });
    document.querySelectorAll(".nav-family").forEach((family) => {
      const active = family.dataset.navFamily === section;
      family.classList.toggle("is-active", active);
      family.classList.toggle("is-open", wideNavigation.matches && active);
    });
    document.querySelectorAll(".nav-item[data-nav-root]").forEach((item) => {
      const family = item.closest(".nav-family");
      if (family) item.setAttribute("aria-expanded", String(family.classList.contains("is-open")));
      else item.removeAttribute("aria-expanded");
    });
    document.querySelectorAll(".nav-children [data-route]").forEach((item) => item.classList.toggle("is-active", item.dataset.route === route));
    document.querySelectorAll(".mobile-navigation-item").forEach((item) => {
      const active = item.dataset.mobileRoute
        ? item.dataset.mobileRoute === section
        : item.dataset.mobileMenu === "commerce"
          ? isCommerce
          : item.dataset.mobileMenu === "partners"
            ? isPartners
            : item.dataset.mobileMenu === "more" && isMore;
      item.classList.toggle("is-active", active);
      if (active) item.setAttribute("aria-current", "page");
      else item.removeAttribute("aria-current");
    });
    document.querySelectorAll("[data-mobile-sheet-item]").forEach((item) => item.classList.toggle("is-active", item.dataset.route === route));
  }
  function updateBreadcrumbs(section, feature, action) {
    const trails = {
      dashboard: ["Pilotage", "Tableau de bord"],
      products: ["Catalogue", "Produits"],
      suppliers: ["Partenaires", "Fournisseurs"],
      clients: ["Partenaires", "Clients"],
      purchases: ["Achats", feature || "Bons de commande"],
      sales: ["Ventes", feature || "Devis"],
      references: ["Configuration", "Référentiels"],
      settings: ["Configuration", "Paramètres"],
    };
    const [mainLabel, featureLabel] = trails[section] || ["Sopmine", feature || "Gestion"];
    const breadcrumbs = document.querySelector("#breadcrumbs");
    const detailSeparator = breadcrumbs.querySelector("[data-breadcrumb-detail-separator]");
    const detailNode = breadcrumbs.querySelector("[data-breadcrumb-detail]");
    Design.Dom.setText(breadcrumbs, "[data-breadcrumb-main]", mainLabel);
    Design.Dom.setText(breadcrumbs, "[data-breadcrumb-section]", featureLabel);
    const mobileContext = document.querySelector("[data-mobile-context]");
    if (mobileContext) mobileContext.textContent = `${mainLabel} / ${featureLabel}`;
    detailSeparator.hidden = !action;
    detailNode.hidden = !action;
    detailNode.textContent = action || "";
    document.title = `Sopmine Sanitaire — ${featureLabel}${action ? ` · ${action}` : ""}`;
  }

  function bindNavigation() {
    document.addEventListener("click", (event) => {
      const route = event.target.closest("[data-route]");
      if (route?.dataset.route) {
        event.preventDefault();
        if (tabletNavigation.matches && route.dataset.navRoot && route.closest(".nav-family")) {
          toggleTabletFamily(route.closest(".nav-family"));
          return;
        }
        closeMobileSheet();
        Design.Router.go(route.dataset.route);
        return;
      }
      const row = event.target.closest("[data-open]");
      if (row && !event.target.closest("button, input, a, select, textarea")) {
        Design.Router.go(row.dataset.open);
        return;
      }
      const action = event.target.closest("[data-action]");
      if (action?.dataset.action === "export") Design.Export.current();
    });
  }

  function applyCommandFilter() {
    if (!command || !commandInput) return;
    const query = Design.Utils.normalizeSearch(commandInput.value);
    command.classList.toggle("has-command-query", Boolean(query));
    const buttons = Array.from(command.querySelectorAll(".command-content button"));
    buttons.forEach((button) => {
      let section = button.previousElementSibling;
      while (section && !section.classList.contains("command-label")) section = section.previousElementSibling;
      const searchText = `${button.textContent} ${button.dataset.route || ""} ${section?.textContent || ""}`;
      const matches = !query || Design.Utils.normalizeSearch(searchText).includes(query);
      button.hidden = button.hasAttribute("data-command-search-only") ? !query || !matches : !matches;
    });
    command.querySelectorAll(".command-label[data-command-search-only]").forEach((label) => {
      if (!query) { label.hidden = true; return; }
      let hasVisibleMatch = false;
      let sibling = label.nextElementSibling;
      while (sibling && !sibling.classList.contains("command-label")) {
        if (sibling.matches("button") && !sibling.hidden) { hasVisibleMatch = true; break; }
        sibling = sibling.nextElementSibling;
      }
      label.hidden = !hasVisibleMatch;
    });
  }

  function toggleTopbarActions() {
    const topbarEnd = document.querySelector(".topbar-end");
    const trigger = document.querySelector("#topbar-more");
    if (!topbarEnd || !trigger) return;
    const open = topbarEnd.classList.toggle("has-topbar-actions-open");
    trigger.setAttribute("aria-expanded", String(open));
    if (open) requestAnimationFrame(() => topbarEnd.querySelector(".topbar-utility-actions button")?.focus());
  }

  function closeTopbarActions(restoreFocus = false) {
    const topbarEnd = document.querySelector(".topbar-end");
    const trigger = document.querySelector("#topbar-more");
    if (!topbarEnd || !trigger) return;
    topbarEnd.classList.remove("has-topbar-actions-open");
    trigger.setAttribute("aria-expanded", "false");
    if (restoreFocus) trigger.focus();
  }
  function bindShellActions() {
    mobileMenu.addEventListener("click", toggleNavigation);
    scrim.addEventListener("click", () => { closeSidebar(true); closeMobileSheet(true); });
    document.querySelector("#topbar-more")?.addEventListener("click", toggleTopbarActions);
    document.addEventListener("click", (event) => {
      if (event.target.closest("#topbar-more, #topbar-utility-actions")) return;
      closeTopbarActions();
    });
    document.querySelector("#mobile-navigation")?.addEventListener("click", (event) => {
      const trigger = event.target.closest("[data-mobile-menu]");
      if (!trigger) return;
      event.preventDefault();
      openMobileSheet(trigger.dataset.mobileMenu, trigger);
    });
    document.querySelector("#mobile-section-sheet")?.addEventListener("click", (event) => {
      if (event.target.closest("[data-mobile-sheet-close]")) {
        closeMobileSheet(true);
        return;
      }
      if (event.target.closest("[data-mobile-sheet-back]")) {
        resetMobileCommerceSheet();
        return;
      }
      const choice = event.target.closest("[data-mobile-commerce-choice]");
      if (choice) showMobileCommerceMode(choice.dataset.mobileCommerceChoice);
    });
    commandTrigger.addEventListener("click", openCommand);
    document.querySelectorAll("[data-command-close]").forEach((button) => button.addEventListener("click", () => closeCommand(true)));
    profileTriggers.forEach((trigger) => trigger.addEventListener("click", () => toggleProfileMenu(trigger)));
    profileMenu.addEventListener("click", handleProfileAction);
    document.addEventListener("click", (event) => {
      if (profileMenu.getAttribute("aria-hidden") === "false" && !event.target.closest("#profile-menu, .profile-menu-trigger")) closeProfileMenu();
    });
    const handleNavigationModeChange = () => {
      document.body.classList.remove("sidebar-open", "mobile-sheet-open");
      scrim.hidden = true;
      if (!wideNavigation.matches) document.body.classList.remove("sidebar-collapsed");
      else if (localStorage.getItem("sopmine-sidebar-collapsed") === "true") document.body.classList.add("sidebar-collapsed");
      closeTopbarActions();
      closeProfileMenu();
      updateSidebarToggle();
      updateNavigation(Design.Shell?.rootSection?.(Design.Router.current()) || "");
    };
    wideNavigation.addEventListener("change", handleNavigationModeChange);
    tabletNavigation.addEventListener("change", handleNavigationModeChange);
    commandInput.addEventListener("input", applyCommandFilter);
    document.querySelector("#theme-toggle").addEventListener("click", () => {
      const theme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
      document.documentElement.dataset.theme = theme;
      localStorage.setItem("sopmine-design-theme", theme);
      updateThemeIcon();
      closeTopbarActions();
    });
    document.addEventListener("keydown", (event) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        openCommand();
      }
      if (event.key === "Escape") {
        closeCommand(true);
        closeSidebar(true);
        closeMobileSheet(true);
        closeTopbarActions(true);
        closeProfileMenu(true);
      }
      const typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement?.tagName || "");
      if (!typing && !event.ctrlKey && !event.metaKey && event.key.toLowerCase() === "p") Design.Router.go("products");
    });
  }

  function toggleNavigation() {
    if (!wideNavigation.matches) {
      openSidebar();
      return;
    }
    document.body.classList.toggle("sidebar-collapsed");
    localStorage.setItem("sopmine-sidebar-collapsed", String(document.body.classList.contains("sidebar-collapsed")));
    closeProfileMenu();
    updateSidebarToggle();
  }

  function updateSidebarToggle() {
    const collapsed = document.body.classList.contains("sidebar-collapsed");
    const mobileOpen = document.body.classList.contains("sidebar-open");
    const expanded = wideNavigation.matches ? !collapsed : mobileOpen;
    mobileMenu.setAttribute("aria-expanded", String(expanded));
    mobileMenu.setAttribute("aria-label", wideNavigation.matches
      ? (collapsed ? "Afficher la navigation" : "Masquer la navigation")
      : "Ouvrir la navigation");
    mobileMenu.replaceChildren(Design.Dom.icon(wideNavigation.matches && !collapsed ? "menu_open" : "menu"));
  }

  function openSidebar() {
    closeMobileSheet();
    document.body.classList.remove("sidebar-collapsed");
    document.body.classList.add("sidebar-open");
    scrim.hidden = false;
    updateSidebarToggle();
    requestAnimationFrame(() => sidebar.querySelector(".nav-item.is-active, .nav-item")?.focus());
  }

  function closeSidebar(restoreFocus = false) {
    const wasOpen = document.body.classList.contains("sidebar-open");
    document.body.classList.remove("sidebar-open");
    if (!document.body.classList.contains("mobile-sheet-open")) scrim.hidden = true;
    updateSidebarToggle();
    if (restoreFocus && wasOpen) mobileMenu.focus();
  }

  function toggleTabletFamily(family) {
    if (!family) return;
    document.querySelectorAll(".nav-family.is-open").forEach((item) => {
      if (item !== family) {
        item.classList.remove("is-open");
        item.querySelector("[data-nav-root]")?.setAttribute("aria-expanded", "false");
      }
    });
    const open = family.classList.toggle("is-open");
    family.querySelector("[data-nav-root]")?.setAttribute("aria-expanded", String(open));
  }

  function openMobileSheet(section, trigger = null) {
    const sheet = document.querySelector("#mobile-section-sheet");
    if (!sheet) return;
    const titles = { partners: "Partenaires", more: "Plus" };
    activeMobileMenu = trigger;
    if (section === "commerce") {
      resetMobileCommerceSheet();
    } else {
      activeMobileCommerceMode = null;
      sheet.dataset.mobileSheetLevel = "options";
      sheet.querySelector("[data-mobile-commerce-choice-list]").hidden = true;
      sheet.querySelector("[data-mobile-sheet-back]").hidden = true;
      sheet.querySelector("[data-mobile-sheet-title]").textContent = titles[section] || "Navigation";
      sheet.querySelectorAll("[data-mobile-sheet-group]").forEach((group) => { group.hidden = group.dataset.mobileSheetGroup !== section; });
    }
    sheet.hidden = false;
    sheet.setAttribute("aria-hidden", "false");
    document.body.classList.add("mobile-sheet-open");
    document.querySelectorAll("[data-mobile-menu]").forEach((item) => item.setAttribute("aria-expanded", String(item === trigger)));
    scrim.hidden = false;
    requestAnimationFrame(() => sheet.querySelector(activeMobileCommerceMode ? "[data-mobile-sheet-back]" : "[data-mobile-sheet-close]")?.focus());
  }

  function resetMobileCommerceSheet() {
    const sheet = document.querySelector("#mobile-section-sheet");
    if (!sheet) return;
    activeMobileCommerceMode = null;
    sheet.dataset.mobileSheetLevel = "choice";
    sheet.querySelector("[data-mobile-commerce-choice-list]").hidden = false;
    sheet.querySelector("[data-mobile-sheet-back]").hidden = true;
    sheet.querySelector("[data-mobile-sheet-title]").textContent = "Commerce";
    sheet.querySelectorAll("[data-mobile-sheet-group]").forEach((group) => { group.hidden = true; });
  }

  function showMobileCommerceMode(mode) {
    const sheet = document.querySelector("#mobile-section-sheet");
    if (!sheet || !["purchases", "sales"].includes(mode)) return;
    activeMobileCommerceMode = mode;
    sheet.dataset.mobileSheetLevel = "options";
    sheet.querySelector("[data-mobile-commerce-choice-list]").hidden = true;
    sheet.querySelector("[data-mobile-sheet-back]").hidden = false;
    sheet.querySelector("[data-mobile-sheet-title]").textContent = mode === "purchases" ? "Achats" : "Ventes";
    sheet.querySelectorAll("[data-mobile-sheet-group]").forEach((group) => { group.hidden = group.dataset.mobileSheetGroup !== mode; });
    requestAnimationFrame(() => sheet.querySelector("[data-mobile-sheet-back]")?.focus());
  }

  function closeMobileSheet(restoreFocus = false) {
    const sheet = document.querySelector("#mobile-section-sheet");
    if (!sheet) return;
    const wasOpen = !sheet.hidden;
    resetMobileCommerceSheet();
    sheet.hidden = true;
    sheet.setAttribute("aria-hidden", "true");
    document.body.classList.remove("mobile-sheet-open");
    document.querySelectorAll("[data-mobile-menu]").forEach((item) => item.setAttribute("aria-expanded", "false"));
    if (!document.body.classList.contains("sidebar-open")) scrim.hidden = true;
    if (restoreFocus && wasOpen) activeMobileMenu?.focus();
    activeMobileMenu = null;
  }
  function openCommand() {
    closeTopbarActions();
    applyCommandFilter();
    command.classList.add("is-open");
    command.setAttribute("aria-hidden", "false");
    commandTrigger.setAttribute("aria-expanded", "true");
    setTimeout(() => commandInput.focus(), 60);
  }

  function closeCommand(restoreFocus = false) {
    const wasOpen = command.classList.contains("is-open");
    command.classList.remove("is-open");
    command.setAttribute("aria-hidden", "true");
    commandTrigger.setAttribute("aria-expanded", "false");
    commandInput.value = "";
    command.classList.remove("has-command-query");
    command.querySelectorAll(".command-content button").forEach((button) => { button.hidden = button.hasAttribute("data-command-search-only"); });
    command.querySelectorAll(".command-label[data-command-search-only]").forEach((label) => { label.hidden = true; });
    if (restoreFocus && wasOpen) commandTrigger.focus();
  }

  function toggleProfileMenu(trigger) {
    if (profileMenu.getAttribute("aria-hidden") === "false" && activeProfileTrigger === trigger) {
      closeProfileMenu(true);
      return;
    }
    openProfileMenu(trigger);
  }

  function openProfileMenu(trigger) {
    closeTopbarActions();
    closeProfileMenu();
    activeProfileTrigger = trigger;
    trigger.setAttribute("aria-expanded", "true");
    profileMenu.classList.add("is-open");
    profileMenu.setAttribute("aria-hidden", "false");
    positionProfileMenu(trigger);
    requestAnimationFrame(() => profileMenu.querySelector("[role='menuitem']")?.focus());
  }

  function positionProfileMenu(trigger) {
    const anchor = trigger.getBoundingClientRect();
    const menu = profileMenu.getBoundingClientRect();
    const preferredLeft = trigger.id === "topbar-profile" ? anchor.right - menu.width : anchor.left;
    const left = Math.max(12, Math.min(preferredLeft, window.innerWidth - menu.width - 12));
    let top = anchor.bottom + 8;
    if (top + menu.height > window.innerHeight - 12) top = Math.max(12, anchor.top - menu.height - 8);
    profileMenu.style.left = `${left}px`;
    profileMenu.style.top = `${top}px`;
  }

  function closeProfileMenu(restoreFocus = false) {
    const trigger = activeProfileTrigger;
    profileMenu.classList.remove("is-open");
    profileMenu.setAttribute("aria-hidden", "true");
    profileTriggers.forEach((item) => item.setAttribute("aria-expanded", "false"));
    activeProfileTrigger = null;
    if (restoreFocus && trigger) trigger.focus();
  }

  function handleProfileAction(event) {
    const action = event.target.closest("[data-profile-action]")?.dataset.profileAction;
    if (!action) return;
    if (action === "settings") {
      closeProfileMenu();
      Design.Router.go("settings/users");
      return;
    }
    if (action === "logout") {
      closeProfileMenu();
      window.SopmineAuth?.clearSession?.();
      window.SopmineAuth?.redirectToLogin?.();
    }
  }

  function updateThemeIcon() {
    const button = document.querySelector("#theme-toggle");
    button.replaceChildren(Design.Dom.icon(document.documentElement.dataset.theme === "dark" ? "light_mode" : "dark_mode"));
  }

  function rootSection(route) {
    if (route.startsWith("product")) return "products";
    if (route.startsWith("supplier")) return "suppliers";
    if (route.startsWith("client")) return "clients";
    if (route.startsWith("purchase")) return "purchases";
    if (route.startsWith("sale")) return "sales";
    return route.split("/")[0];
  }

  Design.Shell = { init, mount, missing, setApiStatus, updateCounts, rootSection };
})();
