(function () {
  const Dom = window.SopmineDesign.Dom;
  window.SopmineDesign.SettingsDom = {
    page: () => Dom.view("settings-page-view"),
    users: () => Dom.view("settings-users-view"),
    userRow: () => Dom.clone("settings-user-row-template"),
    usersEmpty: () => Dom.clone("settings-users-empty-template"),
    numbering: () => Dom.view("settings-numbering-view"),
    nominationGroup: () => Dom.clone("settings-nomination-group-template"),
    nominationItem: () => Dom.clone("settings-nomination-item-template"),
  };
})();
