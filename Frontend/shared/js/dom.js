(function () {
  const Design = window.SopmineDesign;

  function hideDecorativeIcons(root) {
    if (root?.matches?.(".material-symbols-rounded")) root.setAttribute("aria-hidden", "true");
    root?.querySelectorAll?.(".material-symbols-rounded").forEach((node) => node.setAttribute("aria-hidden", "true"));
    return root;
  }

  function clone(id) {
    const template = document.getElementById(id);
    if (!template) throw new Error(`Template introuvable : ${id}`);
    return hideDecorativeIcons(template.content.firstElementChild.cloneNode(true));
  }

  function fragment(id) {
    const template = document.getElementById(id);
    if (!template) throw new Error(`Template introuvable : ${id}`);
    return hideDecorativeIcons(template.content.cloneNode(true));
  }

  function view(id) {
    const node = document.getElementById(id);
    if (!node) throw new Error(`Vue introuvable : ${id}`);
    node.hidden = false;
    return hideDecorativeIcons(node);
  }

  function icon(name) {
    const node = clone("material-icon-template");
    node.textContent = name;
    return node;
  }

  function setText(root, selector, value) {
    const node = root.querySelector(selector);
    if (node) node.textContent = value ?? "";
    return node;
  }

  function setValue(root, selector, value) {
    const node = root.querySelector(selector);
    if (node) node.value = value ?? "";
    return node;
  }

  function setIcon(root, selector, name) {
    const node = root.querySelector(selector);
    if (node) node.replaceChildren(icon(name));
    return node;
  }

  function show(node, visible) {
    if (node) node.hidden = !visible;
  }

  Design.Dom = { clone, fragment, view, icon, setText, setValue, setIcon, show, hideDecorativeIcons };
})();
