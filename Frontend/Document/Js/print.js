(function () {
  const Design = window.SopmineDesign;
  const Store = Design.Store;
  const PRINT_LINE_FIELD_STORAGE_KEY = "sopmine-design-document-line-fields-v4";
  const PRINT_TOTALS_MODE_STORAGE_KEY = "sopmine-design-print-totals-mode-v1";
  const PRINT_TOTALS_MODES = new Set(["detailed", "total", "hidden"]);
  const OPTIONAL_LINE_FIELDS = new Set(["reference", "family", "unit", "vat", "priceHt", "priceTtc", "margin", "totalHt", "totalTtc"]);
  const DEFAULT_LINE_FIELDS = {
    purchase: ["reference", "family", "unit", "vat", "priceTtc", "totalHt", "totalTtc"],
    sale: ["reference", "family", "unit", "vat", "priceHt", "priceTtc", "margin", "totalHt", "totalTtc"],
  };
  const PRINT_OPTIONS = {
    purchase: {
      1: { pdf: true, twoCopies: false, signature: false },
      2: { pdf: true, twoCopies: false, signature: false },
      5: { pdf: true, twoCopies: true, signature: false },
    },
    sale: {
      0: { pdf: true, twoCopies: true, signature: false },
      3: { pdf: true, twoCopies: true, signature: false },
      4: { pdf: true, twoCopies: false, signature: true },
      5: { pdf: true, twoCopies: true, signature: false },
    },
  };

  function resolveOptions(kind, typeValue) {
    return PRINT_OPTIONS[kind]?.[typeValue] || { pdf: false, twoCopies: false, signature: false };
  }

  function normalizeTotalsMode(value) {
    return PRINT_TOTALS_MODES.has(value) ? value : "detailed";
  }

  function getTotalsMode() {
    try {
      return normalizeTotalsMode(localStorage.getItem(PRINT_TOTALS_MODE_STORAGE_KEY));
    } catch {
      return "detailed";
    }
  }

  function setTotalsMode(value) {
    const mode = normalizeTotalsMode(value);
    try {
      localStorage.setItem(PRINT_TOTALS_MODE_STORAGE_KEY, mode);
    } catch { /* Printing still works when storage is unavailable. */ }
    return mode;
  }

  function scopeFor(documentItem) {
    return documentItem.natureValue === 0 ? "purchase" : "sale";
  }

  function visibleLineFields(scope) {
    const key = scope === "purchase" ? "purchase" : "sale";
    let values = DEFAULT_LINE_FIELDS[key];
    try {
      const saved = JSON.parse(localStorage.getItem(PRINT_LINE_FIELD_STORAGE_KEY) || "{}");
      if (Array.isArray(saved?.[key])) values = saved[key];
    } catch { /* Keep the default visible columns. */ }
    const visible = new Set(values.filter((value) => OPTIONAL_LINE_FIELDS.has(value)));
    if (key === "purchase") visible.delete("margin");
    return visible;
  }

  function saveVisibleLineFields(scope, visible) {
    const key = scope === "purchase" ? "purchase" : "sale";
    try {
      const saved = JSON.parse(localStorage.getItem(PRINT_LINE_FIELD_STORAGE_KEY) || "{}");
      saved[key] = [...visible];
      localStorage.setItem(PRINT_LINE_FIELD_STORAGE_KEY, JSON.stringify(saved));
    } catch { /* The preview still works when storage is unavailable. */ }
  }

  function money(value) {
    return `${Number(value || 0).toFixed(2)} MAD`;
  }

  function amount(value) {
    return Number(value || 0).toFixed(2);
  }

  const FRENCH_UNITS = ["zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf"];
  const FRENCH_TENS = ["", "dix", "vingt", "trente", "quarante", "cinquante", "soixante"];

  function frenchUnder100(n) {
    if (n < 20) return FRENCH_UNITS[n];
    if (n < 70) {
      const tens = Math.floor(n / 10);
      const unit = n % 10;
      if (unit === 0) return FRENCH_TENS[tens];
      if (unit === 1) return `${FRENCH_TENS[tens]} et un`;
      return `${FRENCH_TENS[tens]}-${FRENCH_UNITS[unit]}`;
    }
    if (n < 80) return n === 71 ? "soixante et onze" : `soixante-${FRENCH_UNITS[n - 60]}`;
    const remainder = n - 80;
    return remainder === 0 ? "quatre-vingts" : `quatre-vingt-${FRENCH_UNITS[remainder]}`;
  }

  function frenchUnder1000(n) {
    if (n < 100) return frenchUnder100(n);
    const hundreds = Math.floor(n / 100);
    const remainder = n % 100;
    const head = hundreds === 1 ? "cent" : `${FRENCH_UNITS[hundreds]} cent`;
    if (remainder === 0) return hundreds === 1 ? "cent" : `${FRENCH_UNITS[hundreds]} cents`;
    return `${head} ${frenchUnder100(remainder)}`;
  }

  function numberToFrench(n) {
    n = Math.max(0, Math.floor(n));
    if (n < 1000) return frenchUnder1000(n);
    if (n < 1000000) {
      const thousands = Math.floor(n / 1000);
      const remainder = n % 1000;
      const head = thousands === 1 ? "mille" : `${numberToFrench(thousands)} mille`;
      return remainder === 0 ? head : `${head} ${frenchUnder1000(remainder)}`;
    }
    if (n < 1000000000) {
      const millions = Math.floor(n / 1000000);
      const remainder = n % 1000000;
      const head = millions === 1 ? "un million" : `${numberToFrench(millions)} millions`;
      return remainder === 0 ? head : `${head} ${numberToFrench(remainder)}`;
    }
    return String(n);
  }

  function amountInWords(value) {
    const total = Math.round((Number(value) || 0) * 100);
    const dirhams = Math.floor(total / 100);
    const centimes = total % 100;
    let words = `${numberToFrench(dirhams)} ${dirhams > 1 ? "dirhams" : "dirham"}`;
    if (centimes > 0) words += ` et ${numberToFrench(centimes)} ${centimes > 1 ? "centimes" : "centime"}`;
    return words.charAt(0).toUpperCase() + words.slice(1);
  }

  function printDate(documentItem) {
    const [year, month, day] = String(documentItem.dateValue || "").split("-");
    return day && month && year ? `${day}/${month}/${year}` : documentItem.date;
  }

  function columnsFor(documentItem) {
    const scope = scopeFor(documentItem);
    const visible = visibleLineFields(scope);
    const unitTtc = (line) => Number(line.unit || 0) * (1 + Number(line.vat || 0) / 100);
    const values = {
      reference: (line) => line.ref || "—",
      product: (line) => line.product || "—",
      quantity: (line) => Number(line.qty || 0).toFixed(2),
      family: (line) => line.family || "—",
      unit: (line) => line.productUnit || "—",
      vat: (line) => `${Number(line.vat || 0).toFixed(2)} %`,
      priceHt: (line) => amount(Number(line.unit || 0)),
      priceTtc: (line) => amount(unitTtc(line)),
      margin: (line) => {
        const product = Store.byId.product(line.productId);
        const cost = Number(product?.purchase || 0);
        return cost > 0 ? `${(((Number(line.unit || 0) - cost) / cost) * 100).toFixed(1)} %` : "—";
      },
      totalHt: (line) => amount(Number(line.qty || 0) * Number(line.unit || 0)),
      totalTtc: (line) => amount(Number(line.qty || 0) * unitTtc(line)),
    };
    const selected = new Set(visible);
    const order = ["reference", "product", "quantity", "family", "unit", "vat", "priceHt", "priceTtc", "margin", "totalHt", "totalTtc"];
    return order.filter((key) => ["product", "quantity"].includes(key) || selected.has(key)).map((key) => ({ key, value: values[key] }));
  }

  function fillCopy(frameDocument, copy, documentItem, partner, pageLines, pageIndex, totalPages, columns, showSignature, totalsMode) {
    copy.querySelector("[data-print-partner]").textContent = partner.name || "Client";
    copy.querySelector("[data-print-city]").textContent = partner.city || "";
    copy.querySelector("[data-print-page-number]").textContent = `${pageIndex + 1}/${totalPages}`;
    copy.querySelector("[data-print-type]").textContent = documentItem.type;
    copy.querySelector("[data-print-reference]").textContent = documentItem.ref;
    copy.querySelector("[data-print-date]").textContent = printDate(documentItem);
    const note = String(documentItem.notes || "").trim();
    const noteLine = copy.querySelector("[data-print-note-line]");
    if (noteLine) {
      noteLine.hidden = !note;
      copy.querySelector("[data-print-note]").textContent = note;
    }
    const visible = new Set(columns.map((column) => column.key));
    copy.querySelectorAll("[data-print-col]").forEach((cell) => { cell.hidden = !visible.has(cell.dataset.printCol); });
    const lineTemplate = frameDocument.querySelector("#print-line-template");
    const rows = pageLines.map((line) => {
      const row = lineTemplate.content.firstElementChild.cloneNode(true);
      columns.forEach((column) => { row.querySelector(`[data-print-col="${column.key}"]`).textContent = column.value(line); });
      row.querySelectorAll("[data-print-col]").forEach((cell) => { cell.hidden = !visible.has(cell.dataset.printCol); });
      return row;
    });
    copy.querySelector("[data-print-lines]").replaceChildren(...rows);
    const footer = copy.querySelector("[data-print-footer]");
    footer.hidden = pageIndex !== totalPages - 1;
    const totalsHidden = totalsMode === "hidden";
    copy.querySelector("[data-print-total-summary]").hidden = totalsHidden;
    copy.querySelector("[data-print-total-words]").textContent = amountInWords(documentItem.amount);
    copy.querySelector("[data-print-totals]").hidden = totalsHidden;
    copy.querySelectorAll("[data-print-total-detail]").forEach((row) => { row.hidden = totalsMode !== "detailed"; });
    copy.querySelector("[data-print-footer-type]").textContent = documentItem.type;
    copy.querySelector("[data-print-subtotal]").textContent = money(documentItem.subtotal);
    copy.querySelector("[data-print-tax]").textContent = money(documentItem.taxTotal);
    copy.querySelector("[data-print-total-label]").textContent = scopeFor(documentItem) === "sale" && totalsMode === "total" ? "Total" : "Total (TTC)";
    copy.querySelector("[data-print-total]").textContent = money(documentItem.amount);
    copy.querySelector("[data-print-signature]").hidden = !showSignature;
  }

  function fillFrame(iframe, documentItem, partner, { twoCopies, showSignature, totalsMode }) {
    const frameDocument = iframe.contentDocument;
    frameDocument.querySelector("#print-page-orientation").textContent = `@page { size: A4 ${twoCopies ? "landscape" : "portrait"}; margin: 8mm; }`;
    const host = frameDocument.querySelector("#print-pages");
    const pageTemplate = frameDocument.querySelector("#print-page-template");
    const copyTemplate = frameDocument.querySelector("#print-copy-template");
    const lines = Design.DocumentMappers.linesForPresentation(documentItem.lines);
    const linePages = [];
    for (let index = 0; index < lines.length; index += 20) linePages.push(lines.slice(index, index + 20));
    if (!linePages.length) linePages.push([]);
    const columns = columnsFor(documentItem);
    const pages = linePages.map((pageLines, pageIndex) => {
      const page = pageTemplate.content.firstElementChild.cloneNode(true);
      page.classList.toggle("page--two-copies", Boolean(twoCopies));
      const copies = twoCopies ? 2 : 1;
      for (let index = 0; index < copies; index += 1) {
        const copy = copyTemplate.content.firstElementChild.cloneNode(true);
        fillCopy(frameDocument, copy, documentItem, partner, pageLines, pageIndex, linePages.length, columns, showSignature, totalsMode);
        page.appendChild(copy);
      }
      return page;
    });
    host.replaceChildren(...pages);
  }

  function waitForImages(root) {
    return Promise.all([...root.images].map((image) => {
      if (image.complete) return Promise.resolve();
      return new Promise((resolve) => {
        image.addEventListener("load", resolve, { once: true });
        image.addEventListener("error", resolve, { once: true });
      });
    }));
  }

  function fitPreviewFrame(iframe) {
    const frameDocument = iframe.contentDocument;
    if (!frameDocument) return;
    const root = frameDocument.documentElement;
    const body = frameDocument.body;
    const contentHeight = Math.max(
      root?.scrollHeight || 0,
      root?.offsetHeight || 0,
      body?.scrollHeight || 0,
      body?.offsetHeight || 0,
    );
    if (contentHeight > 0) iframe.style.height = `${Math.ceil(contentHeight) + 2}px`;
  }
  function open(documentItem, partner, { twoCopies, showSignature, totalsMode = getTotalsMode(), previewTarget = null }) {
    const iframe = previewTarget
      ? previewTarget.querySelector("[data-document-preview-frame]")
      : document.querySelector("#client-pdf-print-frame");
    if (!iframe) return;
    iframe.hidden = false;
    iframe.style.cssText = previewTarget
      ? "display:block;width:100%;height:auto;aspect-ratio:210 / 297;overflow:hidden;border:0;background:#fff"
      : "position:fixed;width:0;height:0;border:0;visibility:hidden";
    if (previewTarget) iframe.setAttribute("scrolling", "no");
    if (previewTarget) previewTarget.querySelector(".document-paper")?.setAttribute("hidden", "");
    const ready = async () => {
      fillFrame(iframe, documentItem, partner, { twoCopies, showSignature, totalsMode: normalizeTotalsMode(totalsMode) });
      if (previewTarget) {
        await waitForImages(iframe.contentDocument);
        fitPreviewFrame(iframe);
        return;
      }
      await waitForImages(iframe.contentDocument);
      requestAnimationFrame(() => {
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
        setTimeout(() => { iframe.hidden = true; }, 1000);
      });
    };
    const printTemplateReady = () => Boolean(
      iframe.contentDocument?.querySelector("#print-pages")
      && iframe.contentDocument?.querySelector("#print-page-template")
      && iframe.contentDocument?.querySelector("#print-copy-template"),
    );
    if (printTemplateReady()) {
      ready();
    } else {
      const handlePrintFrameLoad = () => {
        if (!printTemplateReady()) return;
        iframe.removeEventListener("load", handlePrintFrameLoad);
        ready();
      };
      iframe.addEventListener("load", handlePrintFrameLoad);
    }
  }

  Design.DocumentPrint = { resolveOptions, getTotalsMode, setTotalsMode, visibleLineFields, saveVisibleLineFields, open };
})();
