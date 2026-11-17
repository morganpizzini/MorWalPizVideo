import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearms,
} from "@morwalpizvideo/services";
import { loader, firearmsShouldRevalidate } from "./loader";
import { loader as componentsCompatibilityLoader } from "../Components/loader";

vi.mock("@morwalpizvideo/services", () => ({
  getWorkshopComponentDefinitions: vi.fn(),
  getWorkshopComponentInstallations: vi.fn(),
  getWorkshopComponents: vi.fn(),
  getWorkshopFirearms: vi.fn(),
}));

beforeEach(() => {
  vi.clearAllMocks();
});

describe("Equipment route data", () => {
  it("loads each equipment collection once", async () => {
    vi.mocked(getWorkshopFirearms).mockResolvedValue([]);
    vi.mocked(getWorkshopComponentDefinitions).mockResolvedValue([]);
    vi.mocked(getWorkshopComponents).mockResolvedValue([]);
    vi.mocked(getWorkshopComponentInstallations).mockResolvedValue([]);

    const result = loader();
    await expect(result.firearms).resolves.toEqual([]);
    await expect(result.definitions).resolves.toEqual([]);
    await expect(result.inventory).resolves.toEqual([]);
    await expect(result.installations).resolves.toEqual([]);
    await expect(result.inventoryPanel).resolves.toEqual([[], [], [], []]);
    expect(getWorkshopFirearms).toHaveBeenCalledTimes(1);
    expect(getWorkshopComponentDefinitions).toHaveBeenCalledTimes(1);
    expect(getWorkshopComponents).toHaveBeenCalledTimes(1);
    expect(getWorkshopComponentInstallations).toHaveBeenCalledTimes(1);
  });

  it("skips revalidation for presentation-only tab and firearm filter changes", () => {
    const args = {
      currentUrl: new URL("https://workshop.local/firearms?filter=active&tab=inventory"),
      nextUrl: new URL("https://workshop.local/firearms?filter=active&tab=catalogue&firearmId=firearm-1"),
      defaultShouldRevalidate: true,
    } as Parameters<typeof firearmsShouldRevalidate>[0];

    expect(firearmsShouldRevalidate(args)).toBe(false);
    expect(
      firearmsShouldRevalidate({
        ...args,
        nextUrl: new URL("https://workshop.local/firearms?filter=archived&tab=catalogue"),
      }),
    ).toBe(true);
    expect(
      firearmsShouldRevalidate({
        ...args,
        defaultShouldRevalidate: false,
      }),
    ).toBe(false);
  });

  it("preserves compatibility query parameters while selecting inventory", () => {
    const response = componentsCompatibilityLoader({
      request: new Request("https://workshop.local/components?firearmId=firearm-1&filter=active"),
    } as never);

    expect(response.status).toBe(302);
    expect(response.headers.get("Location")).toBe(
      "/firearms?firearmId=firearm-1&filter=active&tab=inventory",
    );
  });
});
