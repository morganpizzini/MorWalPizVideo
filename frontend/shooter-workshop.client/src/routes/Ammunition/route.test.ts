import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkshopAmmunitionBatch, WorkshopRecipe } from "@morwalpizvideo/models";
import {
  getWorkshopBatches,
  getWorkshopRecipes,
  recordWorkshopProduction,
} from "@morwalpizvideo/services";
import { action } from "./action";
import { loader } from "./loader";

vi.mock("@morwalpizvideo/services", () => ({
  getWorkshopBatches: vi.fn(),
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

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "production-key" });
});

describe("Ammunition route data functions", () => {
  it("loads recipes and batches", async () => {
    vi.mocked(getWorkshopRecipes).mockResolvedValue(recipes);
    vi.mocked(getWorkshopBatches).mockResolvedValue(batches);

    await expect(loader()).resolves.toEqual({ recipes, batches });
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
      body: new URLSearchParams({
        recipeId: "recipe-1",
        productionDate: "2026-11-15",
        quantityProduced: "250",
        notes: "range session",
      }),
    });

    await expect(action({ request } as never)).resolves.toEqual({ ok: true });
    expect(recordWorkshopProduction).toHaveBeenCalledWith({
      recipeId: "recipe-1",
      productionDate: "2026-11-15",
      quantityProduced: 250,
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
      }),
    });

    await expect(action({ request } as never)).resolves.toEqual({
      ok: false,
      error: "Please review the form.",
    });
    expect(recordWorkshopProduction).not.toHaveBeenCalled();
  });

  it("returns service failures as form feedback", async () => {
    vi.mocked(recordWorkshopProduction).mockRejectedValue(new Error("Recipe was not found."));
    const request = new Request("https://workshop.local/ammunition", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        recipeId: "recipe-1",
        productionDate: "2026-11-15",
        quantityProduced: "250",
      }),
    });

    await expect(action({ request } as never)).resolves.toEqual({
      ok: false,
      error: "Recipe was not found.",
    });
  });
});
