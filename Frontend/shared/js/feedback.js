(() => {
  // Shared feedback helpers. The visual markup lives in the page HTML.
  function getFeedbackClass(type, extraClass = "") {
    return ["feedback", extraClass, `feedback--${type}`].filter(Boolean).join(" ");
  }

  function getHiddenFeedbackClass(extraClass = "") {
    return ["feedback", extraClass, "hidden"].filter(Boolean).join(" ");
  }

  function createFeedback(element, { extraClass = "", autoHideSuccessMs = 0 } = {}) {
    let hideTimer = null;

    function clearTimer() {
      if (hideTimer) window.clearTimeout(hideTimer);
      hideTimer = null;
    }

    function hide() {
      clearTimer();
      if (!element) return;
      element.className = getHiddenFeedbackClass(extraClass);
      element.querySelector("[data-feedback-icon]").textContent = "";
      element.querySelector("[data-feedback-message]").textContent = "";
      const details = element.querySelector("[data-feedback-details]");
      details.replaceChildren();
      details.hidden = true;
    }

    function show(type, message, detailMessages = []) {
      clearTimer();
      if (!element) return;
      element.className = getFeedbackClass(type, extraClass);
      element.querySelector("[data-feedback-icon]").textContent = type === "success" ? "check_circle" : "error";
      element.querySelector("[data-feedback-message]").textContent = message;
      const details = element.querySelector("[data-feedback-details]");
      const template = document.querySelector("#feedback-detail-template");
      details.replaceChildren(...detailMessages.map((detail) => {
        const item = template.content.firstElementChild.cloneNode(true);
        item.textContent = detail;
        return item;
      }));
      details.hidden = !detailMessages.length;
      if (type === "success" && autoHideSuccessMs > 0) hideTimer = window.setTimeout(hide, autoHideSuccessMs);
    }

    return { hide, show };
  }

  function getErrorDetails(data, fallbackMessage) {
    const details = [];
    if (Array.isArray(data?.errors)) {
      data.errors.forEach((error) => {
        if (error?.description) details.push(error.description);
        else if (typeof error === "string" && error.trim()) details.push(error.trim());
      });
    } else if (data?.errors && typeof data.errors === "object") {
      Object.values(data.errors).forEach((messages) => {
        if (Array.isArray(messages)) messages.forEach((message) => { if (message) details.push(String(message)); });
      });
    }
    if (typeof data?.description === "string" && data.description.trim()) details.push(data.description.trim());
    if (typeof data?.title === "string" && data.title.trim() && details.length === 0) details.push(data.title.trim());
    return { message: details[0] || fallbackMessage, details };
  }

  window.SopmineFeedback = Object.freeze({ createFeedback, getErrorDetails });
})();
