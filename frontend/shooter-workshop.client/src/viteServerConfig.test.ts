import { describe, expect, it } from "vitest";
import { getWorkshopViteServerSettings } from "../viteServerConfig";

describe("Workshop Vite server configuration", () => {
  it("uses safe standalone ports when AppHost variables are absent", () => {
    expect(getWorkshopViteServerSettings({})).toEqual({
      port: 5180,
      apiTarget: "https://localhost:7225",
    });
  });

  it("honors the AppHost port and prefers its HTTPS API listener", () => {
    expect(
      getWorkshopViteServerSettings({
        PORT: "5179",
        ASPNETCORE_URLS: "http://localhost:53090;https://localhost:7225",
      }),
    ).toEqual({
      port: 5179,
      apiTarget: "https://localhost:7225",
    });
  });

  it("maps wildcard listener URLs to localhost for local proxying", () => {
    expect(
      getWorkshopViteServerSettings({
        ASPNETCORE_URLS: "https://+:7225;http://*:53090",
      }).apiTarget,
    ).toBe("https://localhost:7225");
  });

  it("rejects malformed AppHost ports and API listener URLs", () => {
    expect(() =>
      getWorkshopViteServerSettings({ PORT: "not-a-port" }),
    ).toThrow(/PORT must be a valid TCP port/);
    expect(() =>
      getWorkshopViteServerSettings({
        ASPNETCORE_URLS: "not-a-listener",
      }),
    ).toThrow(/ASPNETCORE_URLS must contain/);
  });
});
