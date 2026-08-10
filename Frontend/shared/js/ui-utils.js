(() => {
  // Tiny formatting helpers shared across pages. Keeping them here avoids
  // repeating escaping, number formatting, and initials logic in each feature.
  function escapeHtml(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function normalizeText(value) {
    return String(value ?? "").trim();
  }

  function formatNumber(value) {
    return new Intl.NumberFormat("fr-MA", {
      maximumFractionDigits: Number.isInteger(Number(value)) ? 0 : 2,
      minimumFractionDigits: Number.isInteger(Number(value)) ? 0 : 2,
    }).format(Number(value ?? 0));
  }

  function formatMoney(value) {
    return `${formatNumber(value)} MAD`;
  }

  function getInitials(value) {
    const parts = normalizeText(value).split(/\s+/).filter(Boolean).slice(0, 2);

    if (parts.length === 0) {
      return "--";
    }

    return parts.map((part) => part.charAt(0).toUpperCase()).join("");
  }

  window.SopmineUi = Object.freeze({
    escapeHtml,
    normalizeText,
    formatNumber,
    formatMoney,
    getInitials,
  });
})();
