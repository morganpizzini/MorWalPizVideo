import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createWorkshopInventoryProduct,
  recordWorkshopInventoryPurchase,
} from "@morwalpizvideo/services";
import { action } from "./action";

vi.mock("@morwalpizvideo/services", () => ({
  ApiResponseError: class ApiResponseError extends Error {
    status?: number;
    constructor(status?: number) {
      super("API rejected the request.");
      this.status = status;
    }
  },
  adjustWorkshopInventoryLot: vi.fn(),
  correctWorkshopComponentPurchase: vi.fn(),
  createWorkshopInventoryProduct: vi.fn(),
  recordWorkshopComponentPurchase: vi.fn(),
  recordWorkshopInventoryPurchase: vi.fn(),
  setWorkshopInventoryLotStatus: vi.fn(),
}));

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/inventory", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values).toString(),
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("navigator", { onLine: true });
});

describe("Inventory action", () => {
  it("creates an account supply product with a stable action key", async () => {
    await expect(action({
      request: request({
        intent: "create-product",
        idempotencyKey: "inventory-key",
        type: "Bullet",
        brand: "Brand",
        name: "Practice bullet",
      }),
    } as never)).resolves.toEqual({
      ok: true,
      intent: "create-product",
      idempotencyKey: "inventory-key",
    });

    expect(createWorkshopInventoryProduct).toHaveBeenCalledWith({
      type: "Bullet",
      brand: "Brand",
      name: "Practice bullet",
      idempotencyKey: "inventory-key",
    });
  });

  it("does not submit inventory changes while offline", async () => {
    vi.stubGlobal("navigator", { onLine: false });

    await expect(action({
      request: request({
        intent: "record-purchase",
        idempotencyKey: "offline-key",
        productId: "product-1",
        purchaseDate: "2026-11-25",
        quantity: "10",
        totalPrice: "12.50",
        currency: "EUR",
      }),
    } as never)).resolves.toMatchObject({
      ok: false,
      uncertain: false,
      error: "Inventory changes are only available while online.",
    });
    expect(recordWorkshopInventoryPurchase).not.toHaveBeenCalled();
  });

  it("keeps a network-loss purchase outcome explicitly uncertain", async () => {
    vi.mocked(recordWorkshopInventoryPurchase).mockRejectedValue(new Error("Connection lost."));

    await expect(action({
      request: request({
        intent: "record-purchase",
        idempotencyKey: "purchase-key",
        productId: "product-1",
        purchaseDate: "2026-11-25",
        quantity: "10",
        totalPrice: "12.50",
        currency: "EUR",
      }),
    } as never)).resolves.toEqual({
      ok: false,
      intent: "record-purchase",
      idempotencyKey: "purchase-key",
      error: "Connection lost.",
      uncertain: true,
    });
  });
});
