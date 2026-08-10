(() => {
  try {
    const savedTheme = localStorage.getItem("sopmine-design-theme") || localStorage.getItem("sopmine-theme");

    if (savedTheme === "light") {
      document.documentElement.classList.remove("dark");
      document.documentElement.classList.add("theme-light");
    } else {
      document.documentElement.classList.add("dark");
      document.documentElement.classList.remove("theme-light");
    }
  } catch (error) {
    document.documentElement.classList.add("dark");
    document.documentElement.classList.remove("theme-light");
  }
})();
