import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  getWorkshopBatches,
  getWorkshopInventory,
  getWorkshopProductionRecords,
  recordWorkshopProduction,
  updateWorkshopProduction,
} from "@morwalpizvideo/services";
import type { WorkshopInventory, WorkshopProductionRecord } from "@morwalpizvideo/models";
import { submitProduction } from "./submitProduction";

vi.mock("@morwalpizvideo/services", () => ({
  ApiResponseError: class ApiResponseError extends Error {
    status?: number;
    constructor(status?: number) {
      super("API rejected the request.");
      this.status = status;
    }
  },
  getWorkshopBatches: vi.fn(),
  getWorkshopInventory: vi.fn(),
  getWorkshopProductionRecords: vi.fn(),
  recordWorkshopProduction: vi.fn(),
  updateWorkshopProduction: vi.fn(),
}));

const inventory: WorkshopInventory = {
  products: [
    { id: "primer-1", type: "Primer", name: "Primer", archived: false, revision: 0 },
    { id: "powder-1", type: "Powder", name: "Powder", archived: false, revision: 0 },
    { id: "bullet-1", type: "Bullet", name: "Bullet", archived: false, revision: 0 },
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

const session: WorkshopProductionRecord = {
  session: {
    id: "session-1",
    accountId: "account-1",
    recipeId: "recipe-1",
    recipeSnapshot: {
      name: "Practice",
      caliber: "9mm",
      primerProductId: "primer-1",
      powderProductId: "powder-1",
      version: 1,
    },
    productionDate: "2026-01-01",
    quantityProduced: 50,
    batchId: "batch-1",
    bulletProductId: "bullet-1",
    primerLotAllocations: [{ lotId: "primer-lot", quantity: 50 }],
    bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 50 }],
    revision: 1,
  },
  corrections: [],
  projectionPending: false,
};

function form(values: Record<string, string>) {
  const data = new FormData();
  for (const [key, value] of Object.entries(values)) data.set(key, value);
  return data;
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("navigator", { onLine: true });
  vi.mocked(getWorkshopInventory).mockResolvedValue(inventory);
  vi.mocked(getWorkshopProductionRecords).mockResolvedValue([session]);
  vi.mocked(getWorkshopBatches).mockResolvedValue([
    {
      id: "batch-1",
      accountId: "account-1",
      recipeId: "recipe-1",
      recipeSnapshot: session.session.recipeSnapshot,
      productionSessionId: "session-1",
      productionDate: "2026-01-01",
      producedQuantity: 50,
      onHandQuantity: 30,
      revision: 1,
    },
  ]);
});

describe("production action", () => {
  it("submits visible FIFO primer and bullet allocations with the stable idempotency key", async () => {
    await expect(submitProduction(form({
      intent: "produce",
      idempotencyKey: "production-key",
      recipeId: "recipe-1",
      recipeVersion: "1",
      productionDate: "2026-11-25",
      quantityProduced: "75",
      bulletProductId: "bullet-1",
      primerProductId: "primer-1",
      powderProductId: "powder-1",
      primerLotAllocations: '[{"lotId":"primer-lot","quantity":75}]',
      bulletLotAllocations: '[{"lotId":"bullet-lot","quantity":75}]',
      notes: "",
    }))).resolves.toEqual({ ok: true, intent: "produce" });

    expect(recordWorkshopProduction).toHaveBeenCalledWith(expect.objectContaining({
      idempotencyKey: "production-key",
      recipeVersion: 1,
      primerLotAllocations: [{ lotId: "primer-lot", quantity: 75 }],
      bulletLotAllocations: [{ lotId: "bullet-lot", quantity: 75 }],
    }));
  });

  it("prevents reducing a production record below already-consumed units", async () => {
    const result = await submitProduction(form({
      intent: "update-production",
      idempotencyKey: "edit-key",
      sessionId: "session-1",
      expectedRevision: "1",
      recipeId: "recipe-1",
      recipeVersion: "1",
      productionDate: "2026-11-25",
      quantityProduced: "19",
      bulletProductId: "bullet-1",
      primerProductId: "primer-1",
      powderProductId: "powder-1",
      primerLotAllocations: '[{"lotId":"primer-lot","quantity":19}]',
      bulletLotAllocations: '[{"lotId":"bullet-lot","quantity":19}]',
      notes: "",
    }));

    expect(result).toMatchObject({ ok: false, uncertain: false });
    expect(result.ok ? "" : result.error).toContain("20 units already consumed");
    expect(updateWorkshopProduction).not.toHaveBeenCalled();
  });

  it("marks a lost production response uncertain and preserves its retry identity", async () => {
    vi.mocked(recordWorkshopProduction).mockRejectedValue(new Error("Connection lost."));

    await expect(submitProduction(form({
      intent: "produce",
      idempotencyKey: "same-key",
      recipeId: "recipe-1",
      recipeVersion: "1",
      productionDate: "2026-11-25",
      quantityProduced: "10",
      bulletProductId: "bullet-1",
      primerProductId: "primer-1",
      powderProductId: "powder-1",
      primerLotAllocations: '[{"lotId":"primer-lot","quantity":10}]',
      bulletLotAllocations: '[{"lotId":"bullet-lot","quantity":10}]',
      notes: "",
    }))).resolves.toMatchObject({
      ok: false,
      uncertain: true,
      idempotencyKey: "same-key",
    });
  });
});
