import { describe, expect, it } from "vitest";
import { getStorageKey, resolveInitialCulture } from "./i18n";
import { formatDate, formatNumber } from "./i18n-formatters";

describe("Workshop culture resolution", () => {
  it("prefers an explicit supported browser preference", () => {
    const storage = { getItem: () => "en-GB" };
    expect(resolveInitialCulture(storage, ["it-IT"])).toBe("en-GB");
  });

  it("matches the first supported browser language and falls back to Italian", () => {
    expect(resolveInitialCulture({ getItem: () => null }, ["fr-FR", "en-US"])).toBe("en-GB");
    expect(resolveInitialCulture({ getItem: () => "fr-FR" }, ["de-DE"])).toBe("it-IT");
  });

  it("ignores storage failures", () => {
    const storage = { getItem: () => { throw new Error("blocked"); } };
    expect(resolveInitialCulture(storage, ["en-US"])).toBe("en-GB");
    expect(getStorageKey()).toBe("shooter-workshop.culture");
  });
});

describe("Workshop locale formatting", () => {
  it("formats numbers and dates using the selected culture", () => {
    expect(formatNumber(1234567, "it-IT")).toContain(".");
    expect(formatNumber(1234567, "en-GB")).toContain(",");
    expect(formatDate("2026-01-15T00:00:00Z", "it-IT")).toContain("2026");
  });
});
