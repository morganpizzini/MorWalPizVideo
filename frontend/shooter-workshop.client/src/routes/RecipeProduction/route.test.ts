import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkshopInventory } from "@morwalpizvideo/models";
import {
  getWorkshopInventory,
  getWorkshopRecipe,
  recordWorkshopProduction,
} from "@morwalpizvideo/services";
import { action } from "./action";
import { loader } from "./loader";

vi.mock("@morwalpizvideo/services", () => ({
  getWorkshopInventory: vi.fn(),
  getWorkshopRecipe: vi.fn(),
  recordWorkshopProduction: vi.fn(),
}));

const inventory: WorkshopInventory = {
  products: [
    { id: "primer-1", type: "Primer", name: "Primer", archived: false },
    { id: "powder-1", type: "Powder", name: "Powder", archived: false },
    { id: "bullet-1", type: "Bullet", name: "Bullet", archived: false },
  ],
  lots: [
    { id: "primer-lot", productId: "primer-1", purchaseDate: "2026-01-01", purchasedQuantity: 100, availableQuantity: 100, totalPrice: 10, currency: "EUR", isEmpty: false, revision: 1 },
    { id: "bullet-lot", productId: "bullet-1", purchaseDate: "2026-01-01", purchasedQuantity: 100, availableQuantity: 100, totalPrice: 20, currency: "EUR", isEmpty: false, revision: 1 },
  ],
  componentPurchases: [],
  spending: [],
  partialConsumptionCosts: [],
  missingComponentPrices: [],
};

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/recipes/recipe-1/produce", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values),
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "production-key" });
  vi.mocked(getWorkshopInventory).mockResolvedValue(inventory);
});

describe("Recipe production route data functions", () => {
  it("loads the recipe independently of online inventory", async () => {
    const recipe = { id: "recipe-1", status: "Active" };
    vi.mocked(getWorkshopRecipe).mockResolvedValue(recipe as never);

    const resources = loader({ params: { recipeId: "recipe-1" } } as never);
    await expect(resources.recipe).resolves.toBe(recipe);
    await expect(resources.inventory).resolves.toEqual({ inventory, online: true });
    expect(getWorkshopRecipe).toHaveBeenCalledWith("recipe-1");
  });

  it("records production through the existing idempotent command", async () => {
    vi.mocked(recordWorkshopProduction).mockResolvedValue({} as never);

    await expect(
      action({
        request: request({
          intent: "produce",
          idempotencyKey: "production-key",
          recipeId: "recipe-1",
          recipeVersion: "1",
          productionDate: "2026-03-20",
          quantityProduced: "50",
          primerProductId: "primer-1",
          powderProductId: "powder-1",
          bulletProductId: "bullet-1",
          primerLotAllocations: JSON.stringify([{ lotId: "primer-lot", quantity: 50 }]),
          bulletLotAllocations: JSON.stringify([{ lotId: "bullet-lot", quantity: 50 }]),
          notes: "range batch",
        }),
      } as never),
    ).resolves.toEqual({ ok: true, intent: "produce" });

    expect(recordWorkshopProduction).toHaveBeenCalledWith({
      recipeId: "recipe-1",
      recipeVersion: 1,
      productionDate: "2026-03-20",
      quantityProduced: 50,
      bulletProductId: "bullet-1",
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 50 }],
      bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 50 }],
      notes: "range batch",
      idempotencyKey: "production-key",
    });
  });

  it("rejects an invalid production draft without submitting", async () => {
    await expect(
      action({ request: request({ recipeId: "recipe-1", quantityProduced: "0" }) } as never),
    ).resolves.toEqual({
      ok: false,
      error: "Please review the form.",
      uncertain: false,
      intent: "produce",
      idempotencyKey: "",
    });
    expect(recordWorkshopProduction).not.toHaveBeenCalled();
  });
});
