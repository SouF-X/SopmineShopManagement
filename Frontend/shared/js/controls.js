(function () {
  const Design = window.SopmineDesign;
  const records = new WeakMap();
  let active = null;
  let observer = null;
  let observedRoot = null;
  let controlId = 0;

  function controlsIn(root) {
    if (!root) return [];
    const selector = 'select, input[type="date"]';
    return [
      ...(root.matches?.(selector) ? [root] : []),
      ...root.querySelectorAll?.(selector) || [],
    ];
  }

  function isDate(control) {
    return control.matches('input[type="date"]');
  }

  function isSearchableSelect(control) {
    return control.dataset.searchable !== "false";
  }

  function nativeLabel(control) {
    const label = control.labels?.[0];
    const labelText = label?.querySelector(":scope > span, :scope > strong")?.textContent?.trim() || label?.textContent?.trim();
    return control.getAttribute("aria-label") || labelText || (isDate(control) ? "Choisir une date" : "Choisir une option");
  }

  function selectLabel(control) {
    return control.selectedOptions[0]?.textContent?.trim() || control.options[0]?.textContent?.trim() || "Choisir une option";
  }

  function parseDate(value) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value || "")) return null;
    const [year, month, day] = value.split("-").map(Number);
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
  }

  function toIso(date) {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  }

  function formatDate(value) {
    const date = parseDate(value);
    if (!date) return "";
    return `${String(date.getDate()).padStart(2, "0")}/${String(date.getMonth() + 1).padStart(2, "0")}/${date.getFullYear()}`;
  }

  function parseDisplayDate(value) {
    const text = String(value || "").trim();
    const parts = text.split(/\D+/).filter(Boolean);
    if (parts.length === 3) {
      const [day, month, rawYear] = parts;
      const year = rawYear.length === 2 ? `20${rawYear}` : rawYear;
      return parseDate(`${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")}`);
    }
    const digits = text.replace(/\D/g, "");
    if (digits.length !== 6 && digits.length !== 8) return null;
    const day = digits.slice(0, 2);
    const month = digits.slice(2, 4);
    const year = digits.length === 6 ? `20${digits.slice(4, 6)}` : digits.slice(4, 8);
    return parseDate(`${year}-${month}-${day}`);
  }

  function updateTypedDate(record) {
    const date = parseDisplayDate(record.trigger.value);
    if (!date) return;
    const value = toIso(date);
    record.trigger.value = formatDate(value);
    if (record.control.value !== value) {
      record.control.value = value;
      record.control.dispatchEvent(new Event("input", { bubbles: true }));
      record.control.dispatchEvent(new Event("change", { bubbles: true }));
    }
    record.month = new Date(date.getFullYear(), date.getMonth(), 1);
    if (record.popover) {
      renderCalendar(record);
      positionActive();
    }
  }

  function optionSignature(control) {
    return Array.from(control.options, (option) => `${option.value}\u0001${option.text}\u0001${option.disabled}\u0001${option.selected}`).join("\u0002");
  }

  function enhance(control) {
    if (records.has(control) || control.dataset.controlEnhanced === "true") return records.get(control);

    const wrapper = Design.Dom.clone("control-wrapper-template");
    if (control.classList.contains("compact-select")) wrapper.classList.add("control-enhanced--compact");
    const type = isDate(control) ? "date" : "select";
    const trigger = wrapper.querySelector(".control-trigger");
    if (type === "select") {
      trigger.setAttribute("role", "combobox");
      if (isSearchableSelect(control)) {
        trigger.setAttribute("aria-autocomplete", "list");
      } else {
        trigger.readOnly = true;
        trigger.setAttribute("aria-readonly", "true");
      }
    } else {
      trigger.inputMode = "numeric";
      trigger.placeholder = "JJ/MM/AAAA";
    }
    trigger.setAttribute("aria-haspopup", type === "date" ? "dialog" : "listbox");
    trigger.setAttribute("aria-expanded", "false");
    trigger.setAttribute("aria-label", nativeLabel(control));

    const dateIcon = wrapper.querySelector(".control-date-icon");
    dateIcon.hidden = type !== "date";
    const record = { control, wrapper, trigger, type, popover: null, activeIndex: 0, month: null, signature: "", searchQuery: "", listId: `control-options-${++controlId}` };
    records.set(control, record);
    control.dataset.controlEnhanced = "true";
    control.classList.add("control-native");
    control.tabIndex = -1;
    control.setAttribute("aria-hidden", "true");
    control.before(wrapper);
    wrapper.insertBefore(control, trigger);

    if (type === "select") {
      trigger.addEventListener("click", () => {
        if (isSearchableSelect(control)) {
          if (active !== record) open(record);
        } else {
          toggle(record);
        }
      });
      if (isSearchableSelect(control)) {
        trigger.addEventListener("focus", () => trigger.select());
        trigger.addEventListener("input", () => {
          record.searchQuery = trigger.value;
          if (active !== record) open(record, false, true);
          filterSelectOptions(record);
          positionActive();
        });
      }
      trigger.addEventListener("keydown", (event) => handleSelectFieldKeydown(event, record));
    } else {
      trigger.addEventListener("click", () => {
        if (active !== record) open(record);
        trigger.select();
      });
      trigger.addEventListener("focus", () => trigger.select());
      trigger.addEventListener("input", () => updateTypedDate(record));
      trigger.addEventListener("blur", () => {
        if (!parseDisplayDate(trigger.value)) sync(record);
      });
      trigger.addEventListener("keydown", (event) => handleTriggerKeydown(event, record));
    }
    control.addEventListener("change", () => sync(record));
    control.addEventListener("input", () => sync(record));
    control.addEventListener("focus", () => trigger.focus({ preventScroll: true }));
    control.addEventListener("invalid", () => {
      close(false);
      trigger.focus({ preventScroll: true });
    });
    control.form?.addEventListener("reset", () => setTimeout(() => sync(record)));
    sync(record);
    return record;
  }

  function sync(record) {
    const { control, trigger, type } = record;
    trigger.disabled = control.disabled;
    const valueLabel = type === "date" ? formatDate(control.value) : selectLabel(control);
    trigger.setAttribute("aria-label", `${nativeLabel(control)} : ${valueLabel}`);
    if (type === "select") {
      if (active !== record) {
        trigger.value = valueLabel;
        trigger.placeholder = "";
      }
    } else if (document.activeElement !== trigger || parseDisplayDate(trigger.value)) {
      trigger.value = valueLabel;
    }
    if (type === "select") {
      const signature = optionSignature(control);
      if (signature !== record.signature) {
        record.signature = signature;
        if (record.popover) renderSelect(record);
      }
    } else if (record.popover) {
      renderCalendar(record);
    }
  }

  function refresh(root) {
    controlsIn(root).forEach((control) => {
      const record = records.get(control);
      if (record) sync(record);
      else if (!control.disabled) enhance(control);
    });
  }

  function start(root) {
    if (!root || observer) return;
    const observationTarget = document.documentElement;
    if (!observationTarget) return;
    observedRoot = root;
    refresh(root);
    observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        const record = records.get(mutation.target);
        if (record) {
          sync(record);
          return;
        }
        if (mutation.target.nodeType === Node.ELEMENT_NODE && mutation.target.matches?.('select, input[type="date"]') && !mutation.target.disabled) {
          enhance(mutation.target);
        }
        mutation.addedNodes.forEach((node) => {
          if (node.nodeType !== Node.ELEMENT_NODE) return;
          controlsIn(node).forEach((control) => {
            if (!records.has(control) && !control.disabled) enhance(control);
          });
        });
      });
    });
    observer.observe(observationTarget, { childList: true, subtree: true, attributes: true, attributeFilter: ["disabled", "min", "max", "value"] });
    document.addEventListener("pointerdown", handleOutsidePointer, true);
    document.addEventListener("keydown", handleDocumentKeydown, true);
    window.addEventListener("resize", positionActive);
    window.addEventListener("scroll", positionActive, true);
  }

  function toggle(record) {
    if (active === record) {
      close(true);
      return;
    }
    open(record);
  }

  function open(record, focusContent = false, preserveQuery = false) {
    if (record.control.disabled) return;
    close(false);
    active = record;
    if (record.type === "select" && !preserveQuery) {
      record.searchQuery = "";
      if (isSearchableSelect(record.control)) {
        record.trigger.value = "";
        record.trigger.placeholder = record.control.dataset.searchPlaceholder || "Rechercher…";
      } else {
        record.trigger.value = selectLabel(record.control);
        record.trigger.placeholder = "";
      }
    }
    record.trigger.classList.add("is-open");
    record.wrapper.classList.add("is-open");
    record.trigger.setAttribute("aria-expanded", "true");
    const popover = Design.Dom.clone("control-popover-template");
    record.popover = popover;
    record.popoverHost = record.wrapper.closest("dialog[open]");
    (record.popoverHost || document.body).append(popover);
    if (record.type === "select") renderSelect(record);
    else renderCalendar(record);
    positionActive();
    requestAnimationFrame(() => {
      positionActive();
      if (focusContent) focusCurrent(record);
      else if (record.type === "select") record.trigger.focus({ preventScroll: true });
    });
  }

  function close(restoreFocus = false) {
    if (!active) return;
    const record = active;
    active = null;
    record.popover?.remove();
    record.popover = null;
    record.popoverHost = null;
    record.trigger.classList.remove("is-open");
    record.wrapper.classList.remove("is-open");
    record.trigger.setAttribute("aria-expanded", "false");
    if (record.type === "select") {
      record.searchQuery = "";
      record.trigger.value = selectLabel(record.control);
      record.trigger.placeholder = "";
      record.trigger.removeAttribute("aria-controls");
      record.trigger.removeAttribute("aria-activedescendant");
    }
    if (restoreFocus && record.trigger.isConnected) record.trigger.focus({ preventScroll: true });
  }

  function positionActive() {
    if (!active?.popover || !active.trigger.isConnected) return;
    const anchorElement = active.control.dataset.popoverAnchor === "parent"
      ? active.wrapper.parentElement
      : active.trigger;
    const anchor = anchorElement.getBoundingClientRect();
    const popover = active.popover;
    const margin = 8;
    const hostBounds = active.popoverHost?.getBoundingClientRect();
    const viewportLeft = hostBounds?.left ?? 0;
    const viewportTop = hostBounds?.top ?? 0;
    const viewportRight = hostBounds?.right ?? window.innerWidth;
    const viewportBottom = hostBounds?.bottom ?? window.innerHeight;
    const minimumPopoverWidth = active.wrapper.classList.contains("control-enhanced--compact") ? 0 : 180;
    popover.style.width = `${Math.max(minimumPopoverWidth, anchor.width)}px`;
    const availableBelow = Math.max(0, viewportBottom - anchor.bottom - margin - 6);
    const availableAbove = Math.max(0, anchor.top - viewportTop - margin - 6);
    const openUpward = availableAbove > availableBelow;
    const maxPopoverHeight = Math.min(active.type === "date" ? 420 : 320, openUpward ? availableAbove : availableBelow);
    popover.style.height = "auto";
    popover.style.maxHeight = `${maxPopoverHeight}px`;
    const optionList = popover.querySelector(".control-option-list");
    const contentHeight = Math.max(popover.scrollHeight, (optionList?.scrollHeight || 0) + 2);
    const popoverHeight = Math.min(contentHeight, maxPopoverHeight);
    popover.classList.toggle("control-popover--scrollable", contentHeight > maxPopoverHeight);
    popover.style.height = `${popoverHeight}px`;
    const bounds = popover.getBoundingClientRect();
    const width = Math.min(bounds.width, viewportRight - viewportLeft - margin * 2);
    const left = Math.max(viewportLeft + margin, Math.min(anchor.left, viewportRight - width - margin));
    const top = openUpward ? anchor.top - popoverHeight - 6 : anchor.bottom + 6;
    popover.style.width = `${width}px`;
    popover.style.left = `${left - viewportLeft}px`;
    popover.style.top = `${top - viewportTop}px`;
  }

  function renderSelect(record) {
    const { control, popover, trigger } = record;
    if (!popover) return;
    popover.replaceChildren();
    popover.classList.remove("control-popover--calendar");
    popover.classList.add("control-popover--select");
    popover.id = record.listId;
    popover.setAttribute("role", "listbox");
    popover.setAttribute("aria-label", nativeLabel(control));
    trigger.setAttribute("aria-controls", record.listId);

    const list = Design.Dom.clone("control-option-list-template");
    const selectedIndex = Math.max(0, control.selectedIndex);
    record.activeIndex = selectedIndex;
    Array.from(control.options).forEach((option, index) => {
      const item = Design.Dom.clone("control-option-template");
      item.id = `${record.listId}-option-${index}`;
      item.setAttribute("aria-selected", String(option.selected));
      item.dataset.controlIndex = String(index);
      item.disabled = option.disabled;
      item.textContent = option.textContent;
      item.addEventListener("click", () => chooseOption(record, index));
      item.addEventListener("keydown", (event) => handleOptionKeydown(event, record));
      list.append(item);
    });
    popover.append(list);
    filterSelectOptions(record);
  }

  function normalizedSearch(value) {
    return Design.Utils.normalizeSearch(value);
  }

  function optionMatchesSearch(record, index) {
    return normalizedSearch(record.control.options[index]?.textContent).includes(normalizedSearch(record.searchQuery));
  }

  function filterSelectOptions(record) {
    const matching = [];
    record.popover?.querySelectorAll(".control-option").forEach((item) => {
      const index = Number(item.dataset.controlIndex);
      const matches = optionMatchesSearch(record, index);
      item.hidden = !matches;
      if (matches && !item.disabled) matching.push(index);
    });
    if (!matching.includes(record.activeIndex)) record.activeIndex = matching[0] ?? -1;
    updateActiveOption(record);
  }

  function handleSelectFieldKeydown(event, record) {
    if (event.key === "Escape" && active === record) {
      event.preventDefault();
      close(true);
      return;
    }
    if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
      event.preventDefault();
      if (active !== record) open(record, false, true);
      const direction = event.key === "ArrowUp" ? -1 : 1;
      const index = event.key === "Home" ? 0 : event.key === "End" ? record.control.options.length - 1 : record.activeIndex + direction;
      focusOption(record, index, direction, false);
    } else if (event.key === "Enter") {
      event.preventDefault();
      if (active === record && record.activeIndex >= 0) chooseOption(record, record.activeIndex);
      else open(record);
    }
  }

  function chooseOption(record, index) {
    const option = record.control.options[index];
    if (!option || option.disabled) return;
    record.control.value = option.value;
    record.control.dispatchEvent(new Event("change", { bubbles: true }));
    sync(record);
    close(true);
  }

  function handleTriggerKeydown(event, record) {
    if (event.key === "Escape" && active === record) {
      event.preventDefault();
      close(true);
      return;
    }
    if (["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
      event.preventDefault();
      if (active !== record) open(record, true);
      else focusCurrent(record);
      if (record.type === "select" && ["ArrowDown", "ArrowUp"].includes(event.key)) moveOption(record, event.key === "ArrowDown" ? 1 : -1);
    }
  }

  function handleOptionKeydown(event, record) {
    if (event.key === "Escape") {
      event.preventDefault();
      close(true);
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      moveOption(record, 1);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      moveOption(record, -1);
    } else if (event.key === "Home") {
      event.preventDefault();
      focusOption(record, 0, 1);
    } else if (event.key === "End") {
      event.preventDefault();
      focusOption(record, record.control.options.length - 1, -1);
    } else if (["Enter", " "].includes(event.key)) {
      event.preventDefault();
      chooseOption(record, record.activeIndex);
    }
  }

  function moveOption(record, direction) {
    focusOption(record, record.activeIndex + direction, direction);
  }

  function focusOption(record, index, direction, moveFocus = true) {
    const options = record.control.options;
    if (!options.length) return;
    let candidate = Math.max(0, Math.min(index, options.length - 1));
    while ((!options[candidate] || options[candidate].disabled || !optionMatchesSearch(record, candidate)) && candidate + direction >= 0 && candidate + direction < options.length) candidate += direction;
    if (!options[candidate] || options[candidate].disabled || !optionMatchesSearch(record, candidate)) return;
    record.activeIndex = candidate;
    updateActiveOption(record);
    const option = record.popover?.querySelector(`[data-control-index="${candidate}"]`);
    if (moveFocus) option?.focus({ preventScroll: true });
    else option?.scrollIntoView({ block: "nearest" });
  }

  function updateActiveOption(record) {
    const activeOption = record.popover?.querySelector(`[data-control-index="${record.activeIndex}"]`);
    record.popover?.querySelectorAll(".control-option").forEach((item) => item.classList.toggle("is-active", item === activeOption));
    if (record.type === "select" && activeOption) record.trigger.setAttribute("aria-activedescendant", activeOption.id);
    else record.trigger.removeAttribute("aria-activedescendant");
  }

  function focusCurrent(record) {
    if (record.type === "select") {
      focusOption(record, record.activeIndex, 1);
      return;
    }
    const selected = record.control.value || toIso(new Date());
    record.popover?.querySelector(`[data-calendar-day="${selected}"]:not(:disabled)`)?.focus({ preventScroll: true });
  }

  function renderCalendar(record) {
    const { control, popover } = record;
    if (!popover) return;
    const selected = parseDate(control.value);
    if (!record.month) record.month = new Date((selected || new Date()).getFullYear(), (selected || new Date()).getMonth(), 1);
    const month = record.month;
    popover.replaceChildren();
    popover.classList.remove("control-popover--select");
    popover.classList.add("control-popover--calendar");
    popover.setAttribute("role", "dialog");
    popover.setAttribute("aria-label", nativeLabel(control));

    const calendar = Design.Dom.clone("control-calendar-template");
    const previous = calendar.querySelector("[data-calendar-previous]");
    const next = calendar.querySelector("[data-calendar-next]");
    const title = calendar.querySelector("[data-calendar-title]");
    const weekdays = calendar.querySelector("[data-calendar-weekdays]");
    const days = calendar.querySelector("[data-calendar-days]");
    previous.setAttribute("aria-label", "Mois précédent");
    next.setAttribute("aria-label", "Mois suivant");
    previous.addEventListener("click", () => changeMonth(record, -1));
    next.addEventListener("click", () => changeMonth(record, 1));
    title.textContent = new Intl.DateTimeFormat(navigator.language || "fr-FR", { month: "long", year: "numeric" }).format(month);
    previous.disabled = !monthAvailable(record, -1);
    next.disabled = !monthAvailable(record, 1);
    const weekdayBase = new Date(2024, 0, 1);
    for (let index = 0; index < 7; index += 1) {
      const weekday = Design.Dom.clone("control-calendar-weekday-template");
      weekday.textContent = new Intl.DateTimeFormat(navigator.language || "fr-FR", { weekday: "narrow" }).format(new Date(weekdayBase.getFullYear(), weekdayBase.getMonth(), weekdayBase.getDate() + index));
      weekdays.append(weekday);
    }

    const leadingDays = (month.getDay() + 6) % 7;
    for (let index = 0; index < leadingDays; index += 1) days.append(Design.Dom.clone("control-calendar-blank-template"));
    const daysInMonth = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
    const today = toIso(new Date());
    for (let dayNumber = 1; dayNumber <= daysInMonth; dayNumber += 1) {
      const date = new Date(month.getFullYear(), month.getMonth(), dayNumber);
      const iso = toIso(date);
      const day = Design.Dom.clone("control-calendar-day-template");
      day.dataset.calendarDay = iso;
      day.setAttribute("role", "gridcell");
      day.setAttribute("aria-label", new Intl.DateTimeFormat(navigator.language || "fr-FR", { dateStyle: "full" }).format(date));
      day.setAttribute("aria-selected", String(iso === control.value));
      day.classList.toggle("is-today", iso === today);
      day.classList.toggle("is-selected", iso === control.value);
      day.disabled = !dateAllowed(control, iso);
      day.textContent = String(dayNumber);
      day.addEventListener("click", () => chooseDate(record, iso));
      day.addEventListener("keydown", (event) => handleCalendarKeydown(event, record, iso));
      days.append(day);
    }
    popover.append(calendar);
  }

  function dateAllowed(control, value) {
    return (!control.min || value >= control.min) && (!control.max || value <= control.max);
  }

  function monthAvailable(record, offset) {
    const candidate = new Date(record.month.getFullYear(), record.month.getMonth() + offset, 1);
    const first = toIso(candidate);
    const last = toIso(new Date(candidate.getFullYear(), candidate.getMonth() + 1, 0));
    return (!record.control.min || last >= record.control.min) && (!record.control.max || first <= record.control.max);
  }

  function changeMonth(record, offset) {
    if (!monthAvailable(record, offset)) return;
    record.month = new Date(record.month.getFullYear(), record.month.getMonth() + offset, 1);
    renderCalendar(record);
    positionActive();
  }

  function chooseDate(record, value) {
    if (!dateAllowed(record.control, value)) return;
    record.control.value = value;
    record.control.dispatchEvent(new Event("input", { bubbles: true }));
    record.control.dispatchEvent(new Event("change", { bubbles: true }));
    sync(record);
    close(true);
  }

  function handleCalendarKeydown(event, record, value) {
    if (event.key === "Escape") {
      event.preventDefault();
      close(true);
      return;
    }
    const movements = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
    if (movements[event.key]) {
      event.preventDefault();
      focusDate(record, new Date(parseDate(value).getFullYear(), parseDate(value).getMonth(), parseDate(value).getDate() + movements[event.key]));
    } else if (event.key === "PageUp") {
      event.preventDefault();
      changeMonth(record, -1);
    } else if (event.key === "PageDown") {
      event.preventDefault();
      changeMonth(record, 1);
    } else if (["Enter", " "].includes(event.key)) {
      event.preventDefault();
      chooseDate(record, value);
    }
  }

  function focusDate(record, date) {
    record.month = new Date(date.getFullYear(), date.getMonth(), 1);
    renderCalendar(record);
    positionActive();
    const target = record.popover?.querySelector(`[data-calendar-day="${toIso(date)}"]:not(:disabled)`);
    if (target) target.focus({ preventScroll: true });
  }

  function handleOutsidePointer(event) {
    if (!active) return;
    if (active.wrapper.contains(event.target) || active.popover?.contains(event.target)) return;
    close(false);
  }

  function handleDocumentKeydown(event) {
    if (!active) return;
    if (event.key === "Escape") {
      event.preventDefault();
      close(true);
    } else if (event.key === "Tab") {
      close(false);
    }
  }

  Design.Controls = { start, refresh, close };
})();
