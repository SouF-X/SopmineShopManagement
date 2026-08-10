(function () {
  const Design = window.SopmineDesign;
  const usersEndpoint = Design.Api.endpoints.users;
  const nominationsEndpoint = Design.Api.endpoints.documentNominations;

  Design.Api.settings = {
    users: {
      list: () => Design.Api.get(usersEndpoint),
      create: (payload) => Design.Api.send(usersEndpoint, "POST", payload),
      update: (userId, payload) => Design.Api.send(`${usersEndpoint}/${encodeURIComponent(userId)}`, "PUT", payload),
      resetPassword: (userId, newPassword) => Design.Api.send(`${usersEndpoint}/${encodeURIComponent(userId)}/password`, "PUT", { newPassword }),
      changeCurrentPassword: (currentPassword, newPassword) => Design.Api.send(`${usersEndpoint}/me/password`, "PUT", { currentPassword, newPassword }),
      remove: (userId) => Design.Api.remove(`${usersEndpoint}/${encodeURIComponent(userId)}`),
    },
    nominations: {
      list: () => Design.Api.get(nominationsEndpoint),
      update: (key, payload) => Design.Api.send(`${nominationsEndpoint}/${encodeURIComponent(key)}`, "PUT", payload),
    },
  };
})();
