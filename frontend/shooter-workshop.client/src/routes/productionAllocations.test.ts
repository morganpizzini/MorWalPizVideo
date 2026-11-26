import { describe, expect, it } from "vitest";
import type { WorkshopInventory } from "@morwalpizvideo/models";
import { allocateOldestLots, isValidOldestLotAllocation } from "./productionAllocations";

const inventory: WorkshopInventory = {
  products: [],
  lots: [
    { id: "lot-new", productId: "bullet", purchaseDate: "2026-02-01", purchasedQuantity: 100, availableQuantity: 100, totalPrice: 20, currency: "EUR", isEmpty: false, revision: 1 },
    { id: "lot-old", productId: "bullet", purchaseDate: "2026-01-01", purchasedQuantity: 50, availableQuantity: 30, totalPrice: 10, currency: "EUR", isEmpty: false, revision: 2 },
    { id: "lot-empty", productId: "bullet", purchaseDate: "2025-12-01", purchasedQuantity: 10, availableQuantity: 0, totalPrice: 2, currency: "EUR", isEmpty: true, revision: 3 },
    { id: "primer-lot", productId: "primer", purchaseDate: "2026-01-01", purchasedQuantity: 200, availableQuantity: 200, totalPrice: 12, currency: "EUR", isEmpty: false, revision: 1 },
  ],
  componentPurchases: [],
  spending: [],
  partialConsumptionCosts: [],
  missingComponentPrices: [],
};

describe("production lot allocation", () => {
  it("allocates oldest available lots first and splits across lots", () => {
    expect(allocateOldestLots(inventory, "bullet", 40)).toEqual([
      { lotId: "lot-old", quantity: 30 },
      { lotId: "lot-new", quantity: 10 },
    ]);
  });

  it("restores the edited production's previous consumption before reallocating", () => {
    const previous = [{ lotId: "lot-old", quantity: 20 }];
    expect(allocateOldestLots(inventory, "bullet", 45, previous)).toEqual([
      { lotId: "lot-old", quantity: 45 },
    ]);
  });

  it("does not allocate empty lots or accept skipped older lots", () => {
    const selection = [{ lotId: "lot-new", quantity: 10 }];
    expect(isValidOldestLotAllocation(inventory, "bullet", 10, selection)).toBe(false);
    expect(allocateOldestLots(inventory, "bullet", 131)).toEqual([]);
  });
});
