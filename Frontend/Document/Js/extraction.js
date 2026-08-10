(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const MAX_IMAGE_BYTES = 8 * 1024 * 1024;

  async function extract(file, typeValue = 4) {
    validateImage(file);
    return normalizeExtraction(await Design.Api.documents.extractInvoice(file, typeValue));
  }

  function validateImage(file) {
    if (!file?.type?.startsWith("image/")) {
      throw new Error("Choisissez une photo PNG, JPG ou WebP de la facture.");
    }
    if (file.size > MAX_IMAGE_BYTES) {
      throw new Error("La taille maximale est de 8 Mo pour la lecture OpenAI.");
    }
  }

  function normalizeExtraction(dto) {
    const supplier = {
      id: read(dto, "fournisseurId", "FournisseurId", "supplierId") || null,
      name: text(read(dto, "supplierName", "SupplierName", "fournisseur")),
      ice: text(read(dto, "supplierICE", "SupplierICE", "supplierIce", "ice")),
      address: text(read(dto, "supplierAddress", "SupplierAddress", "adresse")),
      city: text(read(dto, "supplierCity", "SupplierCity", "ville")),
      phone: text(read(dto, "supplierPhone", "SupplierPhone", "phone", "telephone")),
      email: text(read(dto, "supplierEmail", "SupplierEmail", "email")),
      website: text(read(dto, "supplierWebsite", "SupplierWebsite", "website", "siteWeb")),
    };
    const typeValue = parseDocumentType(read(dto, "type", "Type", "documentType", "DocumentType"));
    return {
      supplier,
      typeValue,
      typeKey: typeValue === 2 ? "bonreception" : "facture",
      typeLabel: typeValue === 2 ? "Bon de réception" : "Facture fournisseur",
      reference: text(read(dto, "reference", "Reference")),
      dateValue: Design.Utils.isoDate(read(dto, "date", "Date")),
      totalHt: number(read(dto, "totalHT", "TotalHT", "totalHt")),
      totalVat: number(read(dto, "totalTVA", "TotalTVA", "totalTva")),
      totalTtc: number(read(dto, "totalTTC", "TotalTTC", "totalTtc", "total")),
      lines: (read(dto, "lines", "Lines", "lineItems") || []).map(normalizeLine),
    };
  }

  function normalizeLine(line) {
    const vat = number(read(line, "tva", "TVA", "taxRate", "vatRate"));
    const priceHt = nullableNumber(read(line, "unitPriceHT", "UnitPriceHT", "price", "Price", "unitPrice", "UnitPrice"));
    const priceTtc = nullableNumber(read(line, "unitPriceTTC", "UnitPriceTTC", "priceTTC", "PriceTTC"));
    const safePrice = priceHt ?? (priceTtc == null ? 0 : priceTtc / (1 + vat / 100 || 1));
    return {
      productId: read(line, "produitId", "ProduitId", "productId") || null,
      productName: text(read(line, "product", "Product", "productName", "ProductName", "designation")) || "Article extrait",
      productReference: text(read(line, "productReference", "ProductReference", "reference", "ref")),
      productFamily: text(read(line, "productFamily", "ProductFamily", "family", "famille")),
      productUnit: text(read(line, "productUnit", "ProductUnit", "unit", "unite")),
      qty: number(read(line, "quantity", "Quantity", "qty", "qte"), 1),
      unit: Number(safePrice.toFixed(2)),
      unitTtc: priceTtc,
      vat,
      extracted: true,
    };
  }

  function matchSupplier(extraction) {
    if (!extraction) return null;
    const suppliers = Store.state.suppliers;
    const byId = suppliers.find((supplier) => supplier.id === String(extraction.supplier.id || ""));
    if (byId) return byId;
    const ice = comparable(extraction.supplier.ice);
    if (ice) {
      const byIce = suppliers.find((supplier) => comparable(supplier.ice) === ice);
      if (byIce) return byIce;
    }
    const name = comparable(extraction.supplier.name);
    return suppliers.find((supplier) => {
      const supplierName = comparable(supplier.name);
      return supplierName && name && (supplierName === name || supplierName.includes(name) || name.includes(supplierName));
    }) || null;
  }

  function matchProduct(line) {
    const products = Store.state.products;
    const byId = products.find((product) => product.id === String(line.productId || ""));
    if (byId) return byId;
    const reference = comparable(line.productReference);
    if (reference) {
      const byReference = products.find((product) => comparable(product.reference) === reference);
      if (byReference) return byReference;
    }
    const name = comparable(line.productName);
    return products.find((product) => {
      const productName = comparable(product.name);
      return productName && name && (productName === name || productName.includes(name) || name.includes(productName));
    }) || null;
  }

  function parseDocumentType(value) {
    if (Number(value) === 2) return 2;
    const key = String(value || "")
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[^a-z0-9]/gi, "")
      .toLowerCase();
    return ["bonreception", "bonlivraison", "bl", "deliverynote", "supplierdeliverynote"].includes(key) ? 2 : 4;
  }

  function read(source, ...keys) { for (const key of keys) if (source && source[key] !== undefined) return source[key]; }
  function text(value) { return String(value || "").trim(); }
  function number(value, fallback = 0) { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback; }
  function nullableNumber(value) { const parsed = Number(value); return value == null || value === "" || !Number.isFinite(parsed) ? null : parsed; }
  function comparable(value) { return text(value).normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/gi, "").toLowerCase(); }

  Design.DocumentExtraction = { MAX_IMAGE_BYTES, validateImage, extract, normalizeExtraction, matchSupplier, matchProduct };
})();
