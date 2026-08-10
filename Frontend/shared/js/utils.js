(function () {
  const Design = window.SopmineDesign;

  const moneyFormatter = new Intl.NumberFormat("fr-FR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  function money(value) {
    return `${moneyFormatter.format(Number(value || 0))} MAD`;
  }

  function number(value, fallback = 0) {
    const parsed = Number(String(value ?? "").replace(",", "."));
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function optional(value) {
    const normalized = String(value ?? "").trim();
    return normalized || null;
  }

  function normalizeSearch(value) {
    return String(value ?? "")
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/\s+/g, " ")
      .trim()
      .toLocaleLowerCase("fr");
  }

  function initials(value) {
    return String(value || "?")
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase();
  }

  function formatDate(value, fallback = "—") {
    if (!value) return fallback;
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return fallback;
    return new Intl.DateTimeFormat("fr-FR", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(date).replace(".", "");
  }

  function isoDate(value) {
    if (!value) return "";
    const text = String(value);
    const dateOnly = text.match(/^(\d{4}-\d{2}-\d{2})/);
    if (dateOnly) return dateOnly[1];
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    return date.toISOString().slice(0, 10);
  }

  function statusTone(value) {
    const status = String(value || "").toLowerCase();
    if (status.includes("non payée")) return "warning";
    if (status.includes("payé") || status.includes("validé") || status.includes("disponible")) return "success";
    if (status.includes("annulé") || status.includes("rupture")) return "danger";
    if (status.includes("stock bas")) return "warning";
    return "";
  }

  function stock(product) {
    if (product.quantity <= 0) return { label: "Rupture", tone: "danger", ratio: 0 };
    if (product.quantity <= product.minimum) {
      return { label: "Stock bas", tone: "warning", ratio: Math.min(product.quantity / Math.max(product.minimum, 1), 1) };
    }
    return { label: "Disponible", tone: "success", ratio: Math.min(product.quantity / Math.max(product.minimum * 3, 1), 1) };
  }

  function todayAndDue(days = 15) {
    const today = new Date();
    const due = new Date(today);
    due.setDate(due.getDate() + days);
    return { today: isoDate(today), due: isoDate(due) };
  }

  function setSubmitting(form, submitting) {
    form?.querySelectorAll('button[type="submit"]').forEach((button) => {
      button.disabled = submitting;
      button.classList.toggle("is-loading", submitting);
    });
  }

  Design.Utils = {
    money,
    number,
    optional,
    normalizeSearch,
    initials,
    formatDate,
    isoDate,
    statusTone,
    stock,
    todayAndDue,
    setSubmitting,
  };
})();
