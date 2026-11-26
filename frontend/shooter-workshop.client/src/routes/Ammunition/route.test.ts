import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  WorkshopAmmunitionBatch,
  WorkshopInventory,
  WorkshopProductionRecord,
  WorkshopRecipe,
} from "@morwalpizvideo/models";
import {
  getWorkshopBatches,
  getWorkshopInventory,
  getWorkshopProductionRecords,
  getWorkshopRecipes,
  recordWorkshopProduction,
} from "@morwalpizvideo/services";
import { action } from "./action";
import { ammunitionShouldRevalidate, loader } from "./loader";

vi.mock("@morwalpizvideo/services", () => ({
  ApiResponseError: class ApiResponseError extends Error {
    status?: number;
  },
  getWorkshopBatches: vi.fn(),
  getWorkshopInventory: vi.fn(),
  getWorkshopProductionRecords: vi.fn(),
  getWorkshopRecipes: vi.fn(),
  recordWorkshopProduction: vi.fn(),
}));

const recipes = [
  {
    id: "recipe-1",
    accountId: "account-1",
    name: "Practice",
    caliber: "9x19",
    status: "Active",
    version: 1,
    updatedAt: "2026-11-15T00:00:00Z",
  },
] satisfies WorkshopRecipe[];

const batches = [
  {
    id: "batch-1",
    accountId: "account-1",
    recipeId: "recipe-1",
    recipeSnapshot: { name: "Practice", caliber: "9x19", version: 1 },
    productionSessionId: "production-1",
    productionDate: "2026-11-15T00:00:00Z",
    producedQuantity: 250,
    onHandQuantity: 250,
    revision: 0,
  },
] satisfies WorkshopAmmunitionBatch[];

const inventory: WorkshopInventory = {
  products: [
    { id: "primer-1", type: "Primer", name: "Primer", archived: false },
    { id: "powder-1", type: "Powder", name: "Powder", archived: false },
    { id: "bullet-1", type: "Bullet", name: "Bullet", archived: false },
  ],
  lots: [
    { id: "primer-lot", productId: "primer-1", purchaseDate: "2026-01-01", purchasedQuantity: 500, availableQuantity: 500, totalPrice: 50, currency: "EUR", isEmpty: false, revision: 1 },
    { id: "bullet-lot", productId: "bullet-1", purchaseDate: "2026-01-01", purchasedQuantity: 500, availableQuantity: 500, totalPrice: 60, currency: "EUR", isEmpty: false, revision: 1 },
  ],
  componentPurchases: [],
  spending: [],
  partialConsumptionCosts: [],
  missingComponentPrices: [],
};

const productionRecords: WorkshopProductionRecord[] = [];

const validProductionFields = {
  intent: "produce",
  idempotencyKey: "production-key",
  recipeId: "recipe-1",
  recipeVersion: "1",
  productionDate: "2026-11-15",
  quantityProduced: "250",
  bulletProductId: "bullet-1",
  primerProductId: "primer-1",
  powderProductId: "powder-1",
  primerLotAllocations: JSON.stringify([{ lotId: "primer-lot", quantity: 250 }]),
  bulletLotAllocations: JSON.stringify([{ lotId: "bullet-lot", quantity: 250 }]),
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "production-key" });
  vi.mocked(getWorkshopInventory).mockResolvedValue(inventory);
  vi.mocked(getWorkshopProductionRecords).mockResolvedValue(productionRecords);
});

describe("Ammunition route data functions", () => {
  it("skips revalidation when only the selected tab changes", () => {
    const args = {
      currentUrl: new URL("https://workshop.local/ammunition?filter=active"),
      nextUrl: new URL("https://workshop.local/ammunition?filter=active&tab=recipes"),
      defaultShouldRevalidate: true,
    } as Parameters<typeof ammunitionShouldRevalidate>[0];

    expect(ammunitionShouldRevalidate(args)).toBe(false);
    expect(
      ammunitionShouldRevalidate({
        ...args,
        nextUrl: new URL("https://workshop.local/ammunition?filter=active"),
      }),
    ).toBe(true);
  });

  it("keeps revalidation for non-tab changes and action refreshes", () => {
    const args = {
      currentUrl: new URL("https://workshop.local/ammunition?tab=recipes"),
      nextUrl: new URL("https://workshop.local/ammunition?tab=recipes&filter=active"),
      defaultShouldRevalidate: true,
    } as Parameters<typeof ammunitionShouldRevalidate>[0];

    expect(ammunitionShouldRevalidate(args)).toBe(true);
    expect(
      ammunitionShouldRevalidate({
        ...args,
        nextUrl: args.currentUrl,
      }),
    ).toBe(true);
    expect(
      ammunitionShouldRevalidate({
        ...args,
        currentUrl: new URL("https://workshop.local/ammunition"),
        nextUrl: new URL("https://workshop.local/ammunition?tab=recipes"),
        defaultShouldRevalidate: false,
      }),
    ).toBe(false);
  });

  it("loads recipes and batches", async () => {
    vi.mocked(getWorkshopRecipes).mockResolvedValue(recipes);
    vi.mocked(getWorkshopBatches).mockResolvedValue(batches);

    const result = loader();
    await expect(result.recipes).resolves.toEqual(recipes);
    await expect(result.batches).resolves.toEqual(batches);
    await expect(result.inventory).resolves.toEqual({ inventory, online: true });
    await expect(result.productionRecords).resolves.toEqual(productionRecords);
    await expect(result.productionPanel).resolves.toEqual([recipes, batches, productionRecords]);
  });

  it("submits production through the shared service", async () => {
    vi.mocked(recordWorkshopProduction).mockResolvedValue({
      id: "operation-1",
      operationType: "ProductionRecorded",
      effectiveAt: "2026-11-15T00:00:00Z",
      recordedAt: "2026-11-15T00:00:00Z",
      contributionDigest: "digest",
    });

    const request = new Request("https://workshop.local/ammunition", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({ ...validProductionFields, notes: "range session" }).toString(),
    });

    await expect(action({ request } as never)).resolves.toEqual({ ok: true, intent: "produce" });
    expect(recordWorkshopProduction).toHaveBeenCalledWith({
      recipeId: "recipe-1",
      recipeVersion: 1,
      productionDate: "2026-11-15",
      quantityProduced: 250,
      bulletProductId: "bullet-1",
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 250 }],
      bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 250 }],
      notes: "range session",
      idempotencyKey: "production-key",
    });
  });

  it("rejects invalid form data without submitting", async () => {
    const request = new Request("https://workshop.local/ammunition", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        recipeId: "",
        productionDate: "2026-11-15",
        quantityProduced: "0",
      }).toString(),
    });

    await expect(action({ request } as never)).resolves.toEqual({
      ok: false,
      error: "Please review the form.",
      uncertain: false,
      intent: "produce",
      idempotencyKey: "",
    });
    expect(recordWorkshopProduction).not.toHaveBeenCalled();
  });

  it("returns service failures as form feedback", async () => {
    vi.mocked(recordWorkshopProduction).mockRejectedValue(new Error("Recipe was not found."));
    const request = new Request("https://workshop.local/ammunition", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams(validProductionFields).toString(),
    });

    await expect(action({ request } as never)).resolves.toEqual({
      ok: false,
      error: "Recipe was not found.",
      uncertain: true,
      intent: "produce",
      idempotencyKey: "production-key",
    });
  });
});
