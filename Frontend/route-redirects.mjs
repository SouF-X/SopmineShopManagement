const DOCUMENT_TYPES = new Set(["boncommande", "bonreception", "bonlivraison", "devis", "facture", "avoir"]);

function safeId(value) {
  const id = String(value || "").trim();
  return id ? encodeURIComponent(id) : "";
}

function matchesLegacyPath(path, root) {
  return path === `/${root}` || path.startsWith(`/${root}/`);
}

function documentRedirect(url) {
  const purchase = url.searchParams.get("nature")?.toLowerCase() === "achat";
  const area = purchase ? "purchases" : "sales";
  const singular = purchase ? "purchase" : "sale";
  const requestedType = url.searchParams.get("type")?.toLowerCase() || "";
  const type = DOCUMENT_TYPES.has(requestedType) ? requestedType : (purchase ? "boncommande" : "devis");
  const id = safeId(url.searchParams.get("id") || url.searchParams.get("invoiceId"));

  if (id) return `/Document/#${singular}/${id}`;
  if (url.searchParams.get("openDocumentModal") === "1" || /\/editor(?:\.html)?\/?$/i.test(url.pathname)) {
    return `/Document/#${singular}-new/${type}`;
  }
  return `/Document/#${area}/${type}`;
}

export function legacyRedirect(requestUrl) {
  const url = new URL(requestUrl, "http://sopmine.local");
  const path = url.pathname.replace(/\/index\.html$/i, "/").toLowerCase();
  const id = safeId(url.searchParams.get("id"));

  if (matchesLegacyPath(path, "produit")) {
    if (id) return `/Product/#product/${id}`;
    if (url.searchParams.get("openProductModal") === "1") return "/Product/#product-new";
    return "/Product/#products";
  }
  if (matchesLegacyPath(path, "addsupplier")) return "/Supplier/#supplier-new";
  if (matchesLegacyPath(path, "fournisseur")) {
    if (id) return `/Supplier/#supplier/${id}`;
    if (url.searchParams.get("openSupplierModal") === "1") return "/Supplier/#supplier-new";
    return "/Supplier/#suppliers";
  }
  if (path === "/client/" || path.startsWith("/client/js/") || path.startsWith("/client/css/")) return null;
  if (matchesLegacyPath(path, "client")) {
    if (id) return `/Client/#client/${id}`;
    if (url.searchParams.get("openClientModal") === "1") return "/Client/#client-new";
    return "/Client/#clients";
  }
  if (matchesLegacyPath(path, "documents")) return documentRedirect(url);
  if (matchesLegacyPath(path, "parametres")) return "/Settings/#settings/users";
  if (["familles", "unitesmesure"].some((entry) => matchesLegacyPath(path, entry))) {
    return "/Reference/#references";
  }
  return null;
}
