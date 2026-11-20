// @vitest-environment jsdom
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  APPEARANCE_STORAGE_KEY,
  AppearanceProvider,
  setAppearancePreference,
  type AppearancePreference,
} from "@morwalpiz/layout";
import { AppearanceSelector } from "@morwalpiz/layout";

class FakeMediaQueryList extends EventTarget implements MediaQueryList {
  readonly media = "(prefers-color-scheme: light)";
  onchange: ((this: MediaQueryList, event: MediaQueryListEvent) => unknown) | null =
    null;
  private light: boolean;

  constructor(light: boolean) {
    super();
    this.light = light;
  }

  get matches() {
    return this.light;
  }

  addListener(
    callback:
      | ((this: MediaQueryList, event: MediaQueryListEvent) => void)
      | null,
  ) {
    if (callback) this.addEventListener("change", callback as EventListener);
  }

  removeListener(
    callback:
      | ((this: MediaQueryList, event: MediaQueryListEvent) => void)
      | null,
  ) {
    if (callback) this.removeEventListener("change", callback as EventListener);
  }

  setLight(light: boolean) {
    this.light = light;
    const event = new Event("change");
    Object.defineProperty(event, "matches", { value: light });
    this.dispatchEvent(event);
  }
}

let root: Root;
let container: HTMLDivElement;
let mediaQuery: FakeMediaQueryList;

function createStorage() {
  const values = new Map<string, string>();
  return {
    get length() {
      return values.size;
    },
    clear() {
      values.clear();
    },
    getItem(key: string) {
      return values.get(key) ?? null;
    },
    key(index: number) {
      return [...values.keys()][index] ?? null;
    },
    removeItem(key: string) {
      values.delete(key);
    },
    setItem(key: string, value: string) {
      values.set(key, String(value));
    },
  };
}

function renderSelector() {
  root.render(
    <AppearanceProvider>
      <AppearanceSelector
        label="Appearance"
        systemLabel="System"
        lightLabel="Light"
        darkLabel="Dark"
        persistenceErrorMessage="Appearance is temporary because browser storage is unavailable."
      />
    </AppearanceProvider>,
  );
}

async function choosePreference(preference: AppearancePreference) {
  const select = container.querySelector("select");
  expect(select).not.toBeNull();
  await act(async () => {
    select!.value = preference;
    select!.dispatchEvent(new Event("change", { bubbles: true }));
  });
}

beforeEach(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
  Object.defineProperty(window, "localStorage", {
    configurable: true,
    value: createStorage(),
  });
  window.localStorage.clear();
  mediaQuery = new FakeMediaQueryList(false);
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    value: vi.fn(() => mediaQuery),
  });
  setAppearancePreference("system");
  container = document.createElement("div");
  document.body.append(container);
  root = createRoot(container);
});

afterEach(async () => {
  await act(async () => root.unmount());
  container.remove();
  vi.restoreAllMocks();
});

describe("appearance preferences", () => {
  it("follows device changes in System mode and preserves explicit choices", async () => {
    mediaQuery.setLight(true);
    setAppearancePreference("system");
    await act(async () => renderSelector());

    expect(container.querySelector("select")?.value).toBe("system");
    expect(document.documentElement.dataset.appearanceTheme).toBe("light");

    await act(async () => mediaQuery.setLight(false));
    expect(document.documentElement.dataset.appearanceTheme).toBe("dark");

    await choosePreference("light");
    expect(window.localStorage.getItem(APPEARANCE_STORAGE_KEY)).toBe("light");
    await act(async () => mediaQuery.setLight(false));
    expect(document.documentElement.dataset.appearanceTheme).toBe("light");

    await choosePreference("system");
    expect(window.localStorage.getItem(APPEARANCE_STORAGE_KEY)).toBeNull();
    expect(document.documentElement.dataset.appearanceTheme).toBe("dark");
  });

  it("accepts cross-tab changes and falls back to System for invalid values", async () => {
    await act(async () => renderSelector());
    await act(async () =>
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: APPEARANCE_STORAGE_KEY,
          newValue: "light",
        }),
      ),
    );
    expect(container.querySelector("select")?.value).toBe("light");
    expect(document.documentElement.dataset.bsTheme).toBe("light");

    const warning = vi.spyOn(console, "warn").mockImplementation(() => {});
    await act(async () =>
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: APPEARANCE_STORAGE_KEY,
          newValue: "sepia",
        }),
      ),
    );
    expect(container.querySelector("select")?.value).toBe("system");
    expect(warning).toHaveBeenCalledWith(
      "Saved appearance preference is invalid; using the system preference.",
    );
  });

  it("reports when a selected preference cannot be persisted", async () => {
    await act(async () => renderSelector());
    const warning = vi.spyOn(console, "warn").mockImplementation(() => {});
    vi.spyOn(window.localStorage, "setItem").mockImplementation(() => {
      throw new Error("storage disabled");
    });

    await choosePreference("dark");

    expect(container.querySelector('[role="status"]')?.textContent).toContain(
      "Appearance is temporary",
    );
    expect(document.documentElement.dataset.appearanceTheme).toBe("dark");
    expect(warning).toHaveBeenCalledWith(
      "Appearance preference could not be saved to browser storage.",
    );
  });
});

describe("appearance bootstrap parity", () => {
  it("keeps all three pre-hydration scripts identical", () => {
    const scripts = [
      resolve(process.cwd(), "public/appearance-bootstrap.js"),
      resolve(
        process.cwd(),
        "../shooting-range.client/public/appearance-bootstrap.js",
      ),
      resolve(
        process.cwd(),
        "../shooter-workshop.client/public/appearance-bootstrap.js",
      ),
    ].map((path) => readFileSync(path, "utf8"));

    expect(new Set(scripts).size).toBe(1);
    expect(scripts[0]).toContain(APPEARANCE_STORAGE_KEY);
    expect(scripts[0]).toContain("appearanceTheme");
    expect(scripts[0]).toContain("bsTheme");
  });
});
