(function () {
  const Design = window.SopmineDesign;
  const { clone, setText, setIcon } = Design.Dom;

  function pageHeader(node, options) {
    setIcon(node, "[data-page-icon]", options.icon);
    setText(node, "[data-page-eyebrow]", options.eyebrow);
    setText(node, "[data-page-title]", options.title);
    setText(node, "[data-page-description]", options.description);

    const count = node.querySelector("[data-page-count]");
    count.hidden = options.count == null;
    count.textContent = options.count ?? "";

    const secondary = node.querySelector("[data-page-secondary]");
    secondary.hidden = !options.secondaryLabel;
    if (options.secondaryLabel) {
      setIcon(secondary, "[data-action-icon]", options.secondaryIcon || "download");
      setText(secondary, "[data-action-label]", options.secondaryLabel);
      secondary.setAttribute("aria-label", options.secondaryLabel);
      secondary.title = options.secondaryLabel;
      secondary.dataset.action = options.secondaryAction || "export";
    }

    const primary = node.querySelector("[data-page-primary]");
    primary.hidden = !options.actionLabel;
    if (options.actionLabel) {
      setText(primary, "[data-action-label]", options.actionLabel);
      primary.setAttribute("aria-label", options.actionLabel);
      primary.title = options.actionLabel;
      primary.dataset.route = options.actionRoute;
    }
    return node;
  }

  function emptyState(icon, title, description) {
    const node = clone("empty-state-template");
    setIcon(node, "[data-empty-icon]", icon);
    setText(node, "[data-empty-title]", title);
    setText(node, "[data-empty-description]", description);
    return node;
  }

  function apiState({ icon, eyebrow, title, description, retry = false }) {
    const node = clone("api-state-template");
    setIcon(node, "[data-state-icon]", icon);
    setText(node, "[data-state-eyebrow]", eyebrow);
    setText(node, "[data-state-title]", title);
    setText(node, "[data-state-description]", description);
    const button = node.querySelector("[data-api-retry]");
    button.hidden = !retry;
    return node;
  }

  function toast(title, message, tone = "success") {
    const region = document.querySelector("#toast-region");
    const node = clone("toast-template");
    node.classList.add(`toast--${tone}`);
    const iconMap = { error: "error", celebrate: "celebration", warning: "warning" };
    setIcon(node, "[data-toast-icon]", iconMap[tone] || "check");
    setText(node, "[data-toast-title]", title);
    setText(node, "[data-toast-message]", message);
    const remove = () => {
      node.classList.add("is-leaving");
      setTimeout(() => node.remove(), 220);
    };
    node.querySelector("[data-toast-close]").addEventListener("click", remove);
    region.appendChild(node);
    setTimeout(remove, 4200);
  }

  function confirmDelete({ title, target, message }) {
    const dialog = clone("delete-dialog-template");
    setText(dialog, "[data-delete-title]", title || "Confirmer la suppression");
    setText(dialog, "[data-delete-target]", target);
    setText(dialog, "[data-delete-message]", message || "Cet élément sera supprimé de Sopmine.");
    document.body.appendChild(dialog);

    return new Promise((resolve) => {
      let settled = false;
      const finish = (confirmed) => {
        if (settled) return;
        settled = true;
        dialog.classList.remove("is-visible");
        setTimeout(() => {
          dialog.close();
          dialog.remove();
          resolve(confirmed);
        }, 160);
      };

      dialog.querySelector("[data-delete-cancel]").addEventListener("click", () => finish(false));
      dialog.querySelector("[data-delete-confirm]").addEventListener("click", () => finish(true));
      dialog.addEventListener("cancel", (event) => {
        event.preventDefault();
        finish(false);
      });
      dialog.addEventListener("click", (event) => {
        if (event.target === dialog) finish(false);
      });

      dialog.showModal();
      requestAnimationFrame(() => {
        dialog.classList.add("is-visible");
        dialog.querySelector("[data-delete-cancel]").focus();
      });
    });
  }

  function collectionFooter(total, noun, options = {}) {
    const node = clone("collection-footer-template");
    const pageSize = options.pageSize || 10;
    const pageCount = Math.max(1, Math.ceil(total / pageSize));
    const page = Math.min(Math.max(options.page || 1, 1), pageCount);
    const start = total ? (page - 1) * pageSize + 1 : 0;
    const end = Math.min(page * pageSize, total);
    setText(node, "[data-footer-range]", total ? `${start}–${end} sur ${total}` : "0");
    setText(node, "[data-footer-noun]", noun);
    setText(node, "[data-page-current]", `${page} / ${pageCount}`);
    const previous = node.querySelector("[data-page-previous]");
    const next = node.querySelector("[data-page-next]");
    previous.disabled = page <= 1;
    next.disabled = page >= pageCount;
    previous.addEventListener("click", () => options.onPage?.(page - 1));
    next.addEventListener("click", () => options.onPage?.(page + 1));
    return node;
  }

  function isSettledInvoiceStatus(label) {
    return ["payé", "payee", "payée", "payee"].includes(String(label || "").trim().toLocaleLowerCase("fr"));
  }

  function status(label) {
    const node = clone("status-template");
    const isPaid = isSettledInvoiceStatus(label);
    node.className = `status ${Design.Utils.statusTone(label)}${isPaid ? " status--paid" : ""}`.trim();
    if (isPaid) {
      const icon = document.createElement("span");
      icon.className = "material-symbols-rounded";
      icon.setAttribute("aria-hidden", "true");
      icon.textContent = "check_circle";
      node.replaceChildren(icon, document.createTextNode(label));
    } else {
      node.textContent = label;
    }
    return node;
  }

  Design.Components = {
    pageHeader,
    emptyState,
    apiState,
    toast,
    confirmDelete,
    collectionFooter,
    status,
  };
})();
