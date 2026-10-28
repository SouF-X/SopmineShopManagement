import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import vm from "node:vm";

const source = await readFile(
  new URL("../Frontend/shared/js/theme-init.js", import.meta.url),
  "utf8",
);

function runThemeInitializer(savedValues, initialTheme = "light") {
  const classes = new Set();
  const documentElement = {
    dataset: { theme: initialTheme },
    classList: {
      add: (...names) => names.forEach((name) => classes.add(name)),
      contains: (name) => classes.has(name),
      remove: (...names) => names.forEach((name) => classes.delete(name)),
    },
  };

  vm.runInNewContext(source, {
    document: { documentElement },
    localStorage: {
      getItem: (key) => savedValues[key] ?? null,
    },
  });

  return { classes, theme: documentElement.dataset.theme };
}

test("applies a saved dark theme to authenticated-page markup before rendering", () => {
  const result = runThemeInitializer({ "sopmine-design-theme": "dark" });

  assert.equal(result.theme, "dark");
  assert.equal(result.classes.has("dark"), true);
  assert.equal(result.classes.has("theme-light"), false);
});

test("applies a saved light theme consistently across login and authenticated markup", () => {
  const result = runThemeInitializer(
    { "sopmine-design-theme": "light" },
    "dark",
  );

  assert.equal(result.theme, "light");
  assert.equal(result.classes.has("dark"), false);
  assert.equal(result.classes.has("theme-light"), true);
});

test("preserves an authenticated page's light default when no preference is saved", () => {
  const result = runThemeInitializer({});

  assert.equal(result.theme, "light");
  assert.equal(result.classes.has("dark"), false);
  assert.equal(result.classes.has("theme-light"), true);
});
