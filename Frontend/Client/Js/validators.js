(function () {
  function validate(form) {
    return form.reportValidity();
  }

  window.SopmineDesign.ClientValidators = { validate };
})();
