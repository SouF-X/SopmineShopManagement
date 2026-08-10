(function () {
  const Design = window.SopmineDesign;

  function safeImageUrl(value) {
    const url = String(value || "").trim();
    if (!url) return null;
    if (/^data:image\/(png|jpe?g|gif|webp);base64,/i.test(url)) return url;
    try {
      const parsed = new URL(url, location.origin);
      return ["http:", "https:"].includes(parsed.protocol) ? url : null;
    } catch {
      return null;
    }
  }

  function productIcon(dto, index) {
    const value = `${dto.famille || dto.Famille || ""} ${dto.nom || dto.Nom || ""}`
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();
    if (/wc|toilet|cuvette|sanitaire/.test(value)) return "wc";
    if (/baignoire|bath/.test(value)) return "bathtub";
    if (/douche|shower/.test(value)) return "shower";
    if (/filtr|osmose|cartouche|eau/.test(value)) return "water_drop";
    if (/robinet|mitigeur|faucet/.test(value)) return "faucet";
    if (/lavabo|vasque|evier|évier/.test(value)) return "countertops";
    if (/chauffe|thermo|chaudiere/.test(value)) return "water_heater";
    if (/raccord|tuyau|plomb|vanne|pompe/.test(value)) return "plumbing";
    return ["bathroom", "faucet", "plumbing", "water_drop"][index % 4];
  }

  function mapProduct(dto, index = 0) {
    return {
      id: String(dto.produitId || dto.ProduitId || ""),
      createdAt: dto.createdAtUtc || dto.CreatedAtUtc || null,
      reference: String(dto.reference || dto.Reference || "—"),
      name: String(dto.nom || dto.Nom || "Produit sans nom"),
      family: String(dto.famille || dto.Famille || "Sans famille"),
      unit: String(dto.unite || dto.Unite || "Unité"),
      supplierId: dto.fournisseurId || dto.FournisseurId || null,
      imageUrl: safeImageUrl(dto.imageUrl || dto.ImageUrl),
      quantity: Number(dto.quantite ?? dto.Quantite ?? 0),
      minimum: Number(dto.quantiteMini ?? dto.QuantiteMini ?? 0),
      purchase: Number(dto.puAchatHT ?? dto.PuAchatHT ?? 0),
      vat: Number(dto.tva ?? dto.TVA ?? 20),
      margin: Number(dto.marge ?? dto.Marge ?? 0),
      sale: Number(dto.pVenteTTC ?? dto.PVenteTTC ?? 0),
      icon: productIcon(dto, index),
    };
  }

  function toPayload(form) {
    const values = new FormData(form);
    const purchase = Design.Utils.number(values.get("purchase"));
    const vat = Design.Utils.number(values.get("vat"), 20);
    const margin = Design.Utils.number(values.get("margin"));
    const saleHt = purchase * (1 + margin / 100);
    const sale = saleHt * (1 + vat / 100);
    return {
      reference: String(values.get("reference") || "").trim(),
      nom: String(values.get("name") || "").trim(),
      famille: String(values.get("family") || "").trim(),
      unite: String(values.get("unit") || "").trim(),
      fournisseurId: Design.Utils.optional(values.get("supplier")),
      imageUrl: Design.Utils.optional(values.get("imageUrl")),
      quantite: Design.Utils.number(values.get("quantity")),
      quantiteMini: Design.Utils.number(values.get("minimum")),
      puAchatHT: purchase,
      tva: vat,
      marge: Number(margin.toFixed(2)),
      pVenteTTC: sale,
    };
  }

  Design.ProductMappers = { mapProduct, toPayload };
})();
