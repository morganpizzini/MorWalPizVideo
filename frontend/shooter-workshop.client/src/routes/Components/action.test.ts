import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createAndInstallWorkshopComponent,
  recordWorkshopComponentPurchase,
} from "@morwalpizvideo/services";
import { action } from "./action";

vi.mock("@morwalpizvideo/services", () => ({
  archiveWorkshopComponent: vi.fn(),
  archiveWorkshopComponentDefinition: vi.fn(),
  createAndInstallWorkshopComponent: vi.fn(),
  createWorkshopComponentDefinition: vi.fn(),
  deleteWorkshopComponent: vi.fn(),
  deleteWorkshopComponentDefinition: vi.fn(),
  installWorkshopComponent: vi.fn(),
  recordWorkshopComponentPurchase: vi.fn(),
  removeWorkshopComponent: vi.fn(),
  updateWorkshopComponent: vi.fn(),
  updateWorkshopComponentDefinition: vi.fn(),
}));

vi.mock("../../services/offlineStore", () => ({
  componentSnapshot: vi.fn(),
  definitionSnapshot: vi.fn(),
  getOfflineDataset: vi.fn(),
  getRememberedAccountId: vi.fn(() => "account-1"),
  installationSnapshot: vi.fn(),
}));

vi.mock("../../services/offlineMutations", () => ({
  runOrQueueOffline: vi.fn(async (runOnline: () => Promise<unknown>) => ({
    offline: false,
    value: await runOnline(),
  })),
}));

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/firearms", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values).toString(),
  });

const componentForm = (overrides: Record<string, string> = {}) => ({
  intent: "create-component",
  creationIdempotencyKey: "creation-batch",
  definitionId: "definition-1",
  trackingMode: "Counter",
  initialRounds: "0",
  componentQuantity: "1",
  ...overrides,
});

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("navigator", { onLine: true });
  let created = 0;
  vi.mocked(createAndInstallWorkshopComponent).mockImplementation(async () => ({
    component: { id: `physical-${++created}` },
    installation: null,
  } as never));
});

describe("component creation action", () => {
  it("creates distinct physical components for a requested quantity", async () => {
    await expect(action({ request: request(componentForm({ componentQuantity: "3" })) } as never))
      .resolves.toMatchObject({ ok: true, createdCount: 3 });

    expect(createAndInstallWorkshopComponent).toHaveBeenCalledTimes(3);
    expect(createAndInstallWorkshopComponent).toHaveBeenNthCalledWith(
      1, expect.objectContaining({ idempotencyKey: "creation-batch:1", firearmId: null }),
    );
    expect(createAndInstallWorkshopComponent).toHaveBeenNthCalledWith(
      3, expect.objectContaining({ idempotencyKey: "creation-batch:3", firearmId: null }),
    );
  });

  it("records the optional per-component purchase price for every created component", async () => {
    await expect(action({
      request: request(componentForm({
        componentQuantity: "2",
        unitPrice: "12.50",
        purchaseDate: "2026-11-25",
        currency: "eur",
      })),
    } as never)).resolves.toMatchObject({ ok: true, createdCount: 2 });

    expect(recordWorkshopComponentPurchase).toHaveBeenNthCalledWith(1, "physical-1", {
      purchaseDate: "2026-11-25",
      totalPrice: 12.5,
      currency: "EUR",
      idempotencyKey: "creation-batch:1:purchase",
    });
    expect(recordWorkshopComponentPurchase).toHaveBeenNthCalledWith(2, "physical-2", {
      purchaseDate: "2026-11-25",
      totalPrice: 12.5,
      currency: "EUR",
      idempotencyKey: "creation-batch:2:purchase",
    });
  });

  it("rejects quantities outside the supported range before calling the API", async () => {
    await expect(action({
      request: request(componentForm({ componentQuantity: "101" })),
    } as never)).resolves.toMatchObject({
      ok: false,
      error: "Enter a quantity from 1 to 100.",
    });
    expect(createAndInstallWorkshopComponent).not.toHaveBeenCalled();
  });

  it.each([
    ["multiple components", { componentQuantity: "2" }],
    ["a purchase price", {
      unitPrice: "12.50",
      purchaseDate: "2026-11-25",
      currency: "EUR",
    }],
  ])("preserves %s as an explicit online-only error while offline", async (_, fields) => {
    vi.stubGlobal("navigator", { onLine: false });

    await expect(action({
      request: request(componentForm(fields)),
    } as never)).resolves.toMatchObject({
      ok: false,
      error: "Bulk creation and purchase prices require an online connection.",
    });
    expect(createAndInstallWorkshopComponent).not.toHaveBeenCalled();
    expect(recordWorkshopComponentPurchase).not.toHaveBeenCalled();
  });
});
