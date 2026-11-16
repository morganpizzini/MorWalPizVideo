import { beforeEach, describe, expect, it, vi } from "vitest";
import { recordWorkshopMaintenance } from "@morwalpizvideo/services";
import { action } from "./action";

vi.mock("@morwalpizvideo/services", () => ({
  recordWorkshopMaintenance: vi.fn(),
}));

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "generated-key" });
});

describe("Maintenance route action", () => {
  it("preserves the draft idempotency key for uncertain-outcome retries", async () => {
    vi.mocked(recordWorkshopMaintenance).mockResolvedValue({
      id: "maintenance-1",
      accountId: "account-1",
      componentId: "component-1",
      eventType: "Cleaning",
      date: "2026-11-15T10:00:00Z",
      description: "Routine cleaning",
    });

    const request = new Request("https://workshop.local/maintenance", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        targetType: "component",
        targetId: "component-1",
        eventType: "Cleaning",
        date: "2026-11-15T10:00",
        description: "Routine cleaning",
        idempotencyKey: "retry-key",
      }),
    });

    await expect(action({ request } as never)).resolves.toEqual({ ok: true });
    expect(recordWorkshopMaintenance).toHaveBeenCalledWith(expect.objectContaining({
      componentId: "component-1",
      idempotencyKey: "retry-key",
    }));
  });
});
