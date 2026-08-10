(function () {
  const Design = window.SopmineDesign;
  const base = Design.Api.endpoints.invoices;

  Design.Api.documents = {
    list: () => Design.Api.get(base),
    create: (payload) => Design.Api.send(base, "POST", payload),
    update: (id, payload) => Design.Api.send(`${base}/${id}`, "PUT", payload),
    remove: (id) => Design.Api.remove(`${base}/${id}`),
    convertDeliveryNotes: (invoiceIds) => Design.Api.send(`${base}/convert-bon-livraisons`, "POST", { invoiceIds }),
    payments: {
      list: (invoiceId) => Design.Api.get(`${base}/${invoiceId}/payments`),
      create: (invoiceId, payload) => Design.Api.send(`${base}/${invoiceId}/payments`, "POST", payload),
      cancel: (invoiceId, paymentId, payload) => Design.Api.send(`${base}/${invoiceId}/payments/${paymentId}/cancel`, "POST", payload),
    },
    nominations: {
      list: () => Design.Api.get(Design.Api.endpoints.documentNominations),
    },
    extractInvoice(file, typeValue = 4) {
      const form = new FormData();
      form.append("image", file);
      form.append("type", String(typeValue));
      return Design.Api.upload(Design.Api.endpoints.invoiceExtraction, form, {
        timeoutMs: 75 * 1000,
      });
    },
  };
})();
