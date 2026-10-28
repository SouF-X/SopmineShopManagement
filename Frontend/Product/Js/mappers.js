(function () {
  const Design = window.SopmineDesign;

  function finiteNumber(value, fallback = 0) {
    const parsed = Number(String(value ?? "").replace(",", "."));
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function isValidPrice(value) {
    const normalized = String(value ?? "").trim();
    if (!normalized) return true;
    return /^(?:\d+(?:[.,]\d{1,3})?|[.,]\d{1,3})$/.test(normalized);
  }

  function roundMoney(value) {
    return Number(finiteNumber(value).toFixed(3));
  }

  function calculatePricing(values = {}, editedField = "margin") {
    const vat = Math.max(0, finiteNumber(values.vat));
    const taxFactor = 1 + vat / 100;
    let purchase = Math.max(0, finiteNumber(values.purchase));
    let purchaseTtc = Math.max(0, finiteNumber(values.purchaseTtc));
    let saleHt = Math.max(0, finiteNumber(values.saleHt));
    let saleTtc = Math.max(0, finiteNumber(values.saleTtc));
    let margin = Math.max(0, finiteNumber(values.margin));
    const marginFactor = 1 + margin / 100;

    if (editedField === "purchaseTtc") {
      purchase = purchaseTtc / taxFactor;
      saleHt = purchase * marginFactor;
      saleTtc = saleHt * taxFactor;
    } else if (editedField === "saleHt") {
      purchase = saleHt / marginFactor;
      purchaseTtc = purchase * taxFactor;
      saleTtc = saleHt * taxFactor;
    } else if (editedField === "saleTtc") {
      saleHt = saleTtc / taxFactor;
      margin = purchase > 0 ? Math.max(0, (saleHt / purchase - 1) * 100) : 0;
    } else {
      purchaseTtc = purchase * taxFactor;
      saleHt = purchase * marginFactor;
      saleTtc = saleHt * taxFactor;
    }

    return {
      purchase: roundMoney(purchase),
      purchaseTtc: roundMoney(purchaseTtc),
      saleHt: roundMoney(saleHt),
      saleTtc: roundMoney(saleTtc),
      margin: Number(margin.toFixed(2)),
    };
  }

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
    const purchase = finiteNumber(dto.puAchatHT ?? dto.PuAchatHT ?? 0);
    const vat = finiteNumber(dto.tva ?? dto.TVA ?? 20);
    const sale = finiteNumber(dto.pVenteTTC ?? dto.PVenteTTC ?? 0);
    const taxFactor = 1 + Math.max(0, vat) / 100;
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
      purchase,
      purchaseTtc: roundMoney(purchase * taxFactor),
      vat,
      margin: Number(dto.marge ?? dto.Marge ?? 0),
      saleHt: roundMoney(sale / taxFactor),
      sale,
      icon: productIcon(dto, index),
    };
  }

  function toPayload(form) {
    const values = new FormData(form);
    const purchase = roundMoney(values.get("purchase"));
    const vat = Design.Utils.number(values.get("vat"), 20);
    const margin = Number(Design.Utils.number(values.get("margin")).toFixed(2));
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
      pVenteTTC: roundMoney(values.get("saleTtc")),
    };
  }

  Design.ProductMappers = { calculatePricing, isValidPrice, mapProduct, toPayload };
})();
