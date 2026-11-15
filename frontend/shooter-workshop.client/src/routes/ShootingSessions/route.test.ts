import { beforeEach, describe, expect, it, vi } from "vitest";
import { correctWorkshopShooting, recordWorkshopShooting } from "@morwalpizvideo/services";
import { action } from "./action";

vi.mock("@morwalpizvideo/services", () => ({
  correctWorkshopShooting: vi.fn(),
  recordWorkshopShooting: vi.fn(),
}));

const request = (values: Record<string, string>) =>
  new Request("https://workshop.local/sessions/shooting", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams(values),
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal("crypto", { randomUUID: () => "shooting-key" });
});

describe("Shooting sessions route action", () => {
  it("records recipe quantities and preserves the submitted idempotency key", async () => {
    vi.mocked(recordWorkshopShooting).mockResolvedValue({
      id: "operation-1",
      operationType: "ShootingRecorded",
      effectiveAt: "2026-11-15T10:00:00Z",
      recordedAt: "2026-11-15T10:00:00Z",
      contributionDigest: "digest",
    });

    await expect(action({
      request: request({
        intent: "record",
        firearmId: "firearm-1",
        date: "2026-11-15T10:00",
        totalRounds: "50",
        untrackedAmmunition: "10",
        ammunitionRecords: JSON.stringify([{ recipeId: "recipe-1", quantity: 40 }]),
        usageSegments: "[]",
        idempotencyKey: "retry-key",
      }),
    } as never)).resolves.toEqual({ ok: true, intent: "record" });

    expect(recordWorkshopShooting).toHaveBeenCalledWith({
      firearmId: "firearm-1",
      date: "2026-11-15T10:00:00.000Z",
      totalRounds: 50,
      notes: null,
      ammunitionRecords: [{ recipeId: "recipe-1", quantity: 40 }],
      untrackedAmmunition: 10,
      usageSegments: [],
      idempotencyKey: "retry-key",
    });
  });

  it("submits a correction with the original operation and replacement payload", async () => {
    vi.mocked(correctWorkshopShooting).mockResolvedValue({
      id: "operation-2",
      operationType: "ShootingCorrected",
      effectiveAt: "2026-11-15T10:00:00Z",
      recordedAt: "2026-11-15T10:05:00Z",
      contributionDigest: "digest",
    });
    const replacement = {
      firearmId: "firearm-1",
      date: "2026-11-15T10:00:00Z",
      totalRounds: 40,
      untrackedAmmunition: 0,
      ammunitionRecords: [{ recipeId: "recipe-1", quantity: 40 }],
      usageSegments: [],
      idempotencyKey: "replacement-key",
    };

    await expect(action({
      request: request({
        intent: "correct",
        originalOperationId: "operation-1",
        replacementShooting: JSON.stringify(replacement),
        idempotencyKey: "correction-key",
      }),
    } as never)).resolves.toEqual({ ok: true, intent: "correct" });

    expect(correctWorkshopShooting).toHaveBeenCalledWith({
      originalOperationId: "operation-1",
      replacementShooting: replacement,
      idempotencyKey: "correction-key",
    });
  });

  it("rejects invalid quantities before calling the service", async () => {
    await expect(action({
      request: request({
        intent: "record",
        firearmId: "firearm-1",
        date: "2026-11-15T10:00",
        totalRounds: "0",
        untrackedAmmunition: "0",
      }),
    } as never)).resolves.toEqual({ ok: false, error: "Please review the shooting form." });
    expect(recordWorkshopShooting).not.toHaveBeenCalled();
  });
});
