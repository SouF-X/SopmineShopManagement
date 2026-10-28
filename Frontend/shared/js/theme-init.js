(() => {
  try {
    const savedTheme = localStorage.getItem("sopmine-design-theme") || localStorage.getItem("sopmine-theme");
    const markupTheme = ["light", "dark"].includes(document.documentElement.dataset.theme)
      ? document.documentElement.dataset.theme
      : document.documentElement.classList.contains("dark") ? "dark" : "light";
    const theme = ["light", "dark"].includes(savedTheme) ? savedTheme : markupTheme;
    document.documentElement.dataset.theme = theme;

    if (theme === "light") {
      document.documentElement.classList.remove("dark");
      document.documentElement.classList.add("theme-light");
    } else {
      document.documentElement.classList.add("dark");
      document.documentElement.classList.remove("theme-light");
    }
  } catch (error) {
    document.documentElement.dataset.theme = "dark";
    document.documentElement.classList.add("dark");
    document.documentElement.classList.remove("theme-light");
  }
})();
