(function () {
  const Design = window.SopmineDesign;
  const Data = Design.DocumentData;
  let previewUrl = null;
  let selectedFile = null;
  let selectedTypeValue = null;
  let extractionData = null;
  let matchStatus = null;
  let documentForm = null;
  let busy = false;
  let currentStep = "type";

  function render() {
    revokePreview();
    selectedFile = null;
    extractionData = null;
    matchStatus = null;
    documentForm = null;
    busy = false;
    selectedTypeValue = null;
    currentStep = "type";
    const page = Design.DocumentDom.aiWorkspace();
    page.querySelector("[data-document-navigation]").replaceChildren(...Design.DocumentList.renderNavigation("purchases", Data.aiInvoiceWorkspace.key, Design.Store.state.purchases));
    bindFileControls(page);
    renderStage(page);
    window.addEventListener("hashchange", revokePreview, { once: true });
    window.addEventListener("pagehide", revokePreview, { once: true });
    Design.Shell.mount(page, "purchases", "", Data.aiInvoiceWorkspace.title);
  }

  function bindFileControls(page) {
    const input = page.querySelector("[data-ai-file]");
    page.addEventListener("click", (event) => {
      const action = event.target.closest("[data-ai-type], [data-ai-choose], [data-ai-remove], [data-ai-retry], [data-ai-next], [data-ai-back]");
      if (!action) return;
      if (action.matches("[data-ai-type]")) {
        selectedTypeValue = Number(action.dataset.aiType) === 2 ? 2 : 4;
        currentStep = "import";
        renderStage(page);
      } else if (action.matches("[data-ai-choose]")) {
        if (!busy) input.click();
      } else if (action.matches("[data-ai-remove]")) {
        reset(page);
      } else if (action.matches("[data-ai-retry]")) {
        if (selectedFile) analyse(page);
      } else if (action.matches("[data-ai-next]")) {
        advanceToArticles(page);
      } else if (action.matches("[data-ai-back]")) {
        setStep(page, action.dataset.aiBack);
      }
    });
    input.addEventListener("change", () => {
      const file = input.files?.[0];
      input.value = "";
      if (file) selectFile(page, file);
    });
  }

  function selectFile(page, file) {
    try {
      Design.DocumentExtraction.validateImage(file);
    } catch (error) {
      selectedFile = null;
      renderStage(page, { error: error.message });
      return;
    }
    selectedFile = file;
    extractionData = null;
    matchStatus = null;
    documentForm = null;
    revokePreview();
    previewUrl = URL.createObjectURL(file);
    currentStep = "import";
    analyse(page);
  }

  async function analyse(page) {
    if (!selectedFile || busy) return;
    busy = true;
    renderStage(page, { loading: true });
    try {
      const extraction = await Design.DocumentExtraction.extract(selectedFile, selectedTypeValue);
      extraction.typeValue = selectedTypeValue;
      extraction.typeKey = selectedTypeValue === 2 ? "bonreception" : "facture";
      extraction.typeLabel = selectedTypeValue === 2 ? "Bon de réception" : "Facture fournisseur";
      extractionData = extraction;
      const catalogueMode = selectedTypeValue === 2;
      await Design.WorkspacePage.reload();
      matchStatus = {
        supplier: Boolean(Design.DocumentExtraction.matchSupplier(extraction)),
        products: catalogueMode
          ? extraction.lines.map((line) => Boolean(Design.DocumentExtraction.matchProduct(line)))
          : [],
      };
      documentForm = Design.DocumentForms.build(
        "purchase",
        extraction.typeKey,
        null,
        {
          extraction,
          embedded: true,
          catalogueMode,
          requireExistingPartner: selectedTypeValue === 4,
        },
      );
      currentStep = "details";
      renderStage(page);
    } catch (error) {
      renderStage(page, { error: error.message || "Impossible d’analyser cette facture." });
    } finally {
      busy = false;
    }
  }

  function setStep(page, step) {
    if (step === "details" && extractionData && documentForm) {
      currentStep = step;
      renderStage(page);
      return;
    }
    if (step === "import") {
      currentStep = step;
      renderStage(page);
    }
  }

  function advanceToArticles(page) {
    const form = documentForm?.querySelector("[data-document-form]");
    const reference = form?.elements.reference;
    const duplicate = duplicateForReference(reference?.value);
    if (duplicate) {
      reference.setCustomValidity("Cette référence est déjà utilisée par un document enregistré.");
      reference.reportValidity();
      return;
    }
    reference?.setCustomValidity("");
    const required = [form?.elements.partner, form?.elements.date, reference].filter(Boolean);
    if (!required.every((field) => field.reportValidity())) return;
    currentStep = "articles";
    renderStage(page);
  }

  function normalizeReference(value) {
    return String(value ?? "").normalize("NFKC").trim().replace(/\s+/g, " ").toLocaleLowerCase("fr-FR");
  }

  function duplicateForReference(reference) {
    const normalized = normalizeReference(reference);
    if (!normalized) return null;
    return ["purchases", "sales"]
      .flatMap((collection) => Design.Store.state[collection] || [])
      .find((documentItem) => normalizeReference(documentItem?.ref) === normalized) || null;
  }

  function renderStage(page, state = {}) {
    const wizard = page.querySelector("[data-ai-wizard]");
    wizard.dataset.step = currentStep;
    renderProgress(page.querySelector("[data-ai-progress]"), currentStep);
    const stage = page.querySelector("[data-ai-stage]");
    if (!selectedTypeValue || currentStep === "type") {
      stage.replaceChildren(typeChooser());
    } else if (state.loading) {
      stage.replaceChildren(importer(page, { loading: true }));
    } else if (state.error) {
      stage.replaceChildren(importer(page, { error: state.error }));
    } else if (currentStep === "details" && extractionData && documentForm) {
      stage.replaceChildren(detailsStep(page));
    } else if (currentStep === "articles" && extractionData && documentForm) {
      stage.replaceChildren(articlesStep(page));
    } else {
      stage.replaceChildren(importer(page));
    }
  }

  function renderProgress(host, step) {
    const steps = [
      ["type", "Type", "Choisissez le document"],
      ["import", "Importer", "Ajoutez le document"],
      ["details", "V\u00e9rification", "Contr\u00f4lez les informations"],
      ["articles", "Articles & totaux", "Finalisez le document"],
    ];
    const active = Math.max(0, steps.findIndex(([key]) => key === step));
    const list = document.createElement("ol");
    list.className = "invoice-ai-progress-list";
    list.replaceChildren(...steps.map(([key, label, description], index) => {
      const item = document.createElement("li");
      item.className = "invoice-ai-progress-step";
      item.toggleAttribute("data-complete", index < active);
      item.toggleAttribute("data-active", index === active);
      if (index === active) item.setAttribute("aria-current", "step");
      item.innerHTML = '<span class="invoice-ai-progress-number">'
        + (index < active ? '<span class="material-symbols-rounded" aria-hidden="true">check</span>' : index + 1)
        + '</span><span><strong>' + label + '</strong><small>' + description + '</small></span>';
      return item;
    }));
    host.replaceChildren(list);
  }
  function typeChooser() {
    const shell = document.createElement("section");
    shell.className = "invoice-ai-type-chooser";
    shell.innerHTML = '<header class="invoice-ai-step-head"><div><span class="eyebrow"><span class="material-symbols-rounded">category</span> \u00c9tape 1</span><h2>Quel document voulez-vous importer ?</h2><p>S\u00e9lectionnez le type avant de charger le document. Le choix d\u00e9termine uniquement le traitement de l\u2019achat.</p></div></header><div class="invoice-ai-type-options" role="radiogroup" aria-label="Type de document fournisseur"><button class="invoice-ai-type-card" type="button" data-ai-type="2" role="radio" aria-checked="false"><span class="invoice-ai-type-card-icon material-symbols-rounded" aria-hidden="true">inventory</span><span><strong>Bon de r\u00e9ception</strong><small>R\u00e9ception fournisseur et mise \u00e0 jour du stock.</small></span><span class="material-symbols-rounded invoice-ai-type-card-arrow" aria-hidden="true">arrow_forward</span></button><button class="invoice-ai-type-card" type="button" data-ai-type="4" role="radio" aria-checked="false"><span class="invoice-ai-type-card-icon material-symbols-rounded" aria-hidden="true">receipt_long</span><span><strong>Facture fournisseur</strong><small>Enregistrement pour r\u00e8glement et relev\u00e9, sans stock.</small></span><span class="material-symbols-rounded invoice-ai-type-card-arrow" aria-hidden="true">arrow_forward</span></button></div>';
    return shell;
  }
  function isReceptionScan() {
    return selectedTypeValue === 2;
  }

  function selectedDocumentLabel() {
    return isReceptionScan() ? "bon de r\u00e9ception" : "facture fournisseur";
  }

  function selectedDocumentTitle() {
    return isReceptionScan() ? "Bon de r\u00e9ception" : "Facture fournisseur";
  }

  function importer(page, { loading = false, error = "" } = {}) {
    const shell = document.createElement("section");
    shell.className = "invoice-ai-importer";
    const intro = document.createElement("header");
    intro.className = "invoice-ai-step-head";
    intro.innerHTML = '<div><span class="eyebrow"><span class="material-symbols-rounded">document_scanner</span> \u00c9tape 2</span><h2>Importez le ' + selectedDocumentLabel() + '</h2><p>Ajoutez une image lisible. Nous extrairons les informations pour vous laisser les v\u00e9rifier ensuite.</p></div>';
    shell.appendChild(intro);

    if (!selectedFile) {
      shell.appendChild(dropzone());
      if (error) shell.appendChild(errorState(error));
    } else {
      shell.appendChild(sourcePreview({ compact: false }));
      if (loading) shell.appendChild(loadingState());
      else if (error) shell.appendChild(errorState(error));
      else if (extractionData) shell.appendChild(importComplete(page));
    }
    bindDropzone(shell, page);
    return shell;
  }

  function dropzone() {
    const zone = document.createElement("button");
    zone.className = "invoice-ai-dropzone";
    zone.type = "button";
    zone.dataset.aiChoose = "";
    zone.innerHTML = '<span class="invoice-ai-dropzone-icon material-symbols-rounded">upload_file</span><strong>D\u00e9posez votre ' + selectedDocumentLabel() + ' ici</strong><span>PNG, JPG ou WebP \u00b7 8 Mo maximum</span><span class="btn btn-secondary"><span class="material-symbols-rounded">add_photo_alternate</span> Choisir une image</span>';
    return zone;
  }

  function sourcePreview({ compact }) {
    const card = document.createElement("section");
    card.className = "invoice-ai-source-preview" + (compact ? " is-compact" : "");
    const image = document.createElement("img");
    image.src = previewUrl || "";
    image.alt = "Aper\u00e7u du " + selectedDocumentLabel() + " " + (selectedFile?.name || "");
    const meta = document.createElement("div");
    meta.className = "invoice-ai-source-meta";
    meta.innerHTML = '<span class="material-symbols-rounded" aria-hidden="true">image</span><span><strong></strong><small>Document source \u00b7 ' + selectedDocumentTitle() + '</small></span>';
    meta.querySelector("strong").textContent = selectedFile?.name || "Document import\u00e9";
    const actions = document.createElement("div");
    actions.className = "invoice-ai-source-actions";
    actions.innerHTML = '<button class="btn btn-secondary" type="button" data-ai-choose><span class="material-symbols-rounded">sync</span> Remplacer</button><button class="btn btn-secondary" type="button" data-ai-remove><span class="material-symbols-rounded">delete</span> Retirer</button>';
    card.append(image, meta, actions);
    return card;
  }

  function loadingState() {
    const state = document.createElement("section");
    state.className = "invoice-ai-analysis-state";
    state.innerHTML = '<span class="invoice-ai-analysis-icon material-symbols-rounded" aria-hidden="true">document_search</span><div><span class="eyebrow">Extraction en cours</span><h3>Lecture du ' + selectedDocumentLabel() + '</h3><p>Nous relevons le fournisseur, les dates, les montants et les articles.</p></div><div class="invoice-ai-analysis-lines" aria-hidden="true"><i></i><i></i><i></i></div>';
    return state;
  }

  function errorState(message) {
    const state = document.createElement("section");
    state.className = "invoice-ai-analysis-state is-error";
    state.innerHTML = '<span class="invoice-ai-analysis-icon material-symbols-rounded" aria-hidden="true">error</span><div><span class="eyebrow">Analyse impossible</span><h3>Le document n\u2019a pas pu \u00eatre lu</h3><p></p></div><div class="invoice-ai-stage-actions">' + (selectedFile ? '<button class="btn btn-secondary" type="button" data-ai-retry><span class="material-symbols-rounded">refresh</span> R\u00e9essayer</button>' : "") + '<button class="btn btn-primary" type="button" data-ai-choose><span class="material-symbols-rounded">upload_file</span> Choisir une autre image</button></div>';
    state.querySelector("p").textContent = message;
    return state;
  }

  function importComplete() {
    const state = document.createElement("section");
    state.className = "invoice-ai-analysis-state is-complete";
    state.innerHTML = '<span class="invoice-ai-analysis-icon material-symbols-rounded" aria-hidden="true">task_alt</span><div><span class="eyebrow">Analyse termin\u00e9e</span><h3>Votre ' + selectedDocumentLabel() + ' est pr\u00eat \u00e0 \u00eatre v\u00e9rifi\u00e9</h3><p>Les donn\u00e9es extraites sont disponibles \u00e0 l\u2019\u00e9tape suivante.</p></div><div class="invoice-ai-stage-actions"><button class="btn btn-primary" type="button" data-ai-back="details"><span class="material-symbols-rounded">arrow_forward</span> V\u00e9rifier les informations</button></div>';
    return state;
  }

  function detailsStep() {
    const shell = document.createElement("section");
    shell.className = "invoice-ai-flow invoice-ai-flow--details";
    shell.innerHTML = '<header class="invoice-ai-step-head"><div><span class="eyebrow"><span class="material-symbols-rounded">business</span> \u00c9tape 3</span><h2>Fournisseur & document</h2><p>Contr\u00f4lez la date, la r\u00e9f\u00e9rence et les informations extraites avant de passer aux articles.</p></div></header><div class="invoice-ai-details-layout"><aside class="invoice-ai-compact-preview" data-ai-compact-preview></aside><div class="invoice-ai-form-host" data-ai-form-host></div></div><footer class="invoice-ai-flow-actions"><button class="btn btn-secondary" type="button" data-ai-back="import"><span class="material-symbols-rounded">arrow_back</span> Retour \u00e0 l\u2019import</button><button class="btn btn-primary" type="button" data-ai-next>Confirmer les informations <span class="material-symbols-rounded">arrow_forward</span></button></footer>';
    shell.querySelector("[data-ai-compact-preview]").appendChild(sourcePreview({ compact: true }));
    shell.querySelector("[data-ai-compact-preview]").appendChild(associationSummary());
    const nextButton = shell.querySelector("[data-ai-next]");
    if (!isReceptionScan() && !matchStatus?.supplier) nextButton.disabled = true;
    const formHost = shell.querySelector("[data-ai-form-host]");
    const warning = document.createElement("div");
    warning.className = "invoice-ai-duplicate-warning";
    warning.dataset.aiDuplicateWarning = "";
    warning.hidden = true;
    warning.setAttribute("role", "status");
    warning.setAttribute("aria-live", "polite");
    formHost.append(warning, documentForm);
    bindDuplicateReferenceWarning(shell);
    return shell;
  }

  function bindDuplicateReferenceWarning(shell) {
    const form = documentForm?.querySelector("[data-document-form]");
    const reference = form?.elements.reference;
    const warning = shell.querySelector("[data-ai-duplicate-warning]");
    const next = shell.querySelector("[data-ai-next]");
    if (!reference || !warning || !next) return;

    documentForm._duplicateReferenceAbort?.abort();
    const controller = new AbortController();
    documentForm._duplicateReferenceAbort = controller;
    const update = () => {
      const duplicate = duplicateForReference(reference.value);
      next.disabled = Boolean(duplicate);
      warning.hidden = !duplicate;
      warning.replaceChildren();
      if (!duplicate) return;

      const icon = document.createElement("span");
      icon.className = "material-symbols-rounded";
      icon.setAttribute("aria-hidden", "true");
      icon.textContent = "warning";
      const copy = document.createElement("div");
      const title = document.createElement("strong");
      title.textContent = "Document d\u00e9j\u00e0 enregistr\u00e9";
      const message = document.createElement("p");
      message.textContent = `La référence « ${reference.value.trim()} » est déjà utilisée. Modifiez-la uniquement s’il s’agit d’un document différent.`;
      copy.append(title, message);
      warning.append(icon, copy);
    };
    reference.addEventListener("input", update, { signal: controller.signal });
    update();
  }

  function articlesStep() {
    const shell = document.createElement("section");
    shell.className = "invoice-ai-flow invoice-ai-flow--articles";
    shell.innerHTML = '<header class="invoice-ai-step-head"><div><span class="eyebrow"><span class="material-symbols-rounded">list_alt</span> \u00c9tape 4</span><h2>Articles & totaux</h2><p>V\u00e9rifiez les lignes extraites, les prix et le total avant d\u2019enregistrer le ' + selectedDocumentLabel() + '.</p></div></header><div class="invoice-ai-form-host" data-ai-form-host></div><footer class="invoice-ai-flow-actions"><button class="btn btn-secondary" type="button" data-ai-back="details"><span class="material-symbols-rounded">arrow_back</span> Retour aux d\u00e9tails</button></footer>';
    shell.querySelector("[data-ai-form-host]").appendChild(documentForm);
    return shell;
  }

  function associationSummary() {
    const summary = document.createElement("section");
    if (!isReceptionScan()) {
      const supplierMatched = Boolean(matchStatus?.supplier);
      summary.className = "invoice-ai-association " + (supplierMatched ? "is-existing" : "is-new");
      summary.innerHTML = '<span class="material-symbols-rounded" aria-hidden="true">' + (supplierMatched ? "link" : "link_off") + '</span><div><span>' + (supplierMatched ? "Fournisseur existant" : "Fournisseur introuvable") + '</span><strong></strong><small>' + (supplierMatched
        ? extractionData.lines.length + ' article' + (extractionData.lines.length > 1 ? 's' : '') + ' conservé' + (extractionData.lines.length > 1 ? 's' : '') + ' sur la facture, sans catalogue ni stock.'
        : 'Cette facture ne pourra pas être ajoutée tant que le fournisseur extrait n’existe pas dans le catalogue.') + '</small></div>';
      summary.querySelector("strong").textContent = supplierMatched
        ? "Fournisseur · " + (extractionData.supplier.name || "Associé")
        : "Ajoutez d’abord · " + (extractionData.supplier.name || "Fournisseur extrait");
      return summary;
    }

    const matchedProducts = matchStatus.products.filter(Boolean).length;
    summary.className = "invoice-ai-association" + (matchStatus.supplier ? " is-existing" : " is-new");
    summary.innerHTML = '<span class="material-symbols-rounded" aria-hidden="true">' + (matchStatus.supplier ? "link" : "person_add") + '</span><div><span>' + (matchStatus.supplier ? "Association existante" : "Association \u00e0 confirmer") + '</span><strong></strong><small>' + matchedProducts + ' article' + (matchedProducts > 1 ? 's' : '') + ' reconnu' + (matchedProducts > 1 ? 's' : '') + ' sur ' + extractionData.lines.length + '</small></div>';
    summary.querySelector("strong").textContent = matchStatus.supplier
      ? "Fournisseur \u00b7 " + (extractionData.supplier.name || "Associ\u00e9")
      : "Nouveau fournisseur \u00b7 " + (extractionData.supplier.name || "\u00c0 s\u00e9lectionner");
    return summary;
  }

  function bindDropzone(shell, page) {
    const zone = shell.querySelector("[data-ai-choose]");
    if (!zone || !zone.classList.contains("invoice-ai-dropzone")) return;
    zone.addEventListener("dragover", (event) => {
      event.preventDefault();
      if (!busy) zone.classList.add("is-dragging");
    });
    zone.addEventListener("dragleave", () => zone.classList.remove("is-dragging"));
    zone.addEventListener("drop", (event) => {
      event.preventDefault();
      zone.classList.remove("is-dragging");
      const file = event.dataTransfer?.files?.[0];
      if (file && !busy) selectFile(page, file);
    });
  }

  function reset(page) {
    if (busy) return;
    revokePreview();
    selectedFile = null;
    extractionData = null;
    matchStatus = null;
    documentForm = null;
    selectedTypeValue = null;
    currentStep = "type";
    renderStage(page);
  }

  function revokePreview() {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    previewUrl = null;
  }

  Design.DocumentAiWorkspace = { render };
})();
