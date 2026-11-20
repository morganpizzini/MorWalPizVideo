(function () {
  var storageKey = "morwalpiz.appearance.v1";
  var preference = "system";

  try {
    var stored = window.localStorage.getItem(storageKey);
    if (stored === "system" || stored === "light" || stored === "dark") {
      preference = stored;
    } else if (stored !== null) {
      console.warn("Saved appearance preference is invalid; using the system preference.");
    }
  } catch {
    console.warn("Appearance preference could not be read from browser storage.");
  }

  var systemPrefersLight = false;
  try {
    if (typeof window.matchMedia === "function") {
      systemPrefersLight = window.matchMedia("(prefers-color-scheme: light)").matches;
    }
  } catch {
    systemPrefersLight = false;
  }

  var resolved = preference === "system" ? (systemPrefersLight ? "light" : "dark") : preference;
  document.documentElement.dataset.appearanceTheme = resolved;
  document.documentElement.dataset.bsTheme = resolved;
})();
