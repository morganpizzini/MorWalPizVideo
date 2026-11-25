import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiResponseError, recordWorkshopMaintenance } from "@morwalpizvideo/services";
import {
  clearAllOfflineData,
  createOfflineCommand,
  enqueueOfflineCommand,
  getOfflineState,
  markOfflinePrepared,
  rememberOfflineAccount,
} from "../services/offlineStore";
import { action } from "./offlineAction";
import type { ActionFunctionArgs } from "react-router";

vi.mock("@morwalpizvideo/services", async importOriginal => {
  const actual = await importOriginal<typeof import("@morwalpizvideo/services")>();
  return {
    ...actual,
    recordWorkshopMaintenance: vi.fn(),
  };
});

const account = {
  id: "account-sync",
  username: "range-user",
  email: "range@example.test",
  displayName: "Range User",
};

async function queueMaintenance() {
  await rememberOfflineAccount(account);
  await markOfflinePrepared(account.id);
  await enqueueOfflineCommand(
    account.id,
    createOfflineCommand("maintenance-create", {
      request: {
        firearmId: "firearm-1",
        eventType: "Cleaning",
        date: "2025-01-01T10:00:00.000Z",
        roundCountAtEvent: null,
        description: "Cleaned at range",
        notes: null,
        idempotencyKey: "maintenance-sync-1",
      },
    }),
  );
}

function syncRequest() {
  return new Request("https://workshop.local/", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({ intent: "sync" }),
  });
}

beforeEach(async () => {
  vi.clearAllMocks();
  await clearAllOfflineData();
  localStorage.clear();
});

describe("offline command synchronization", () => {
  it("sends a queued operation and removes it after the server accepts it", async () => {
    await queueMaintenance();
    vi.mocked(recordWorkshopMaintenance).mockResolvedValue({} as never);

    const result = await action({ request: syncRequest() } as ActionFunctionArgs);

    expect(result).toMatchObject({ ok: true, processed: 1 });
    expect(recordWorkshopMaintenance).toHaveBeenCalledWith(
      expect.objectContaining({ idempotencyKey: "maintenance-sync-1" }),
    );
    expect(await getOfflineState(account.id)).toMatchObject({ pending: 0, blocked: 0 });
  });

  it("blocks a rejected command and preserves its conflict explanation", async () => {
    await queueMaintenance();
    vi.mocked(recordWorkshopMaintenance).mockRejectedValue(
      new ApiResponseError({ status: 409, errors: ["Revision conflict"] }),
    );

    const result = await action({ request: syncRequest() } as ActionFunctionArgs);
    const state = await getOfflineState(account.id);

    expect(result).toMatchObject({ ok: true, processed: 0 });
    expect(state).toMatchObject({ pending: 0, blocked: 1 });
    expect(state.commands[0].error).toContain("Revision conflict");
  });

  it("preserves queued work and requests login again after session expiry", async () => {
    await queueMaintenance();
    vi.mocked(recordWorkshopMaintenance).mockRejectedValue(
      new ApiResponseError({ status: 401, errors: ["Session expired"] }),
    );

    const result = await action({ request: syncRequest() } as ActionFunctionArgs);

    expect(result).toMatchObject({ ok: false, authRequired: true, processed: 0 });
    expect(await getOfflineState(account.id)).toMatchObject({ pending: 1, blocked: 0 });
  });
});
