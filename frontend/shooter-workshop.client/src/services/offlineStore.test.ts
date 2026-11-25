import { beforeEach, describe, expect, it } from "vitest";
import {
  clearAllOfflineData,
  createOfflineCommand,
  enqueueOfflineCommand,
  getOfflineDataset,
  getOfflineState,
  getRememberedAccountId,
  hasOfflineData,
  markOfflinePrepared,
  rememberOfflineAccount,
  saveOfflineDataset,
} from "./offlineStore";

const account = {
  id: "account-1",
  username: "range-user",
  email: "range@example.test",
  displayName: "Range User",
};

beforeEach(async () => {
  await clearAllOfflineData();
  localStorage.clear();
});

describe("Workshop offline workspace", () => {
  it("persists prepared data and projects queued changes without crossing accounts", async () => {
    await rememberOfflineAccount(account);
    await Promise.all([
      saveOfflineDataset(account.id, "definitions", [
        { id: "definition-1", name: "Existing" },
      ]),
      saveOfflineDataset(account.id, "firearms", [
        { id: "firearm-1", manufacturer: "Maker" },
      ]),
    ]);
    await markOfflinePrepared(account.id);
    await enqueueOfflineCommand(
      account.id,
      createOfflineCommand(
        "definition-create",
        { name: "New definition" },
        { definitions: { upsert: [{ id: "local:definition-1", name: "New definition" }] } },
      ),
    );

    expect(getRememberedAccountId()).toBe(account.id);
    expect(await getOfflineDataset("another-account", "definitions")).toBeNull();
    expect(await getOfflineDataset(account.id, "firearms")).toEqual([
      { id: "firearm-1", manufacturer: "Maker" },
    ]);
    expect(await hasOfflineData()).toBe(true);
    expect(await getOfflineDataset(account.id, "definitions")).toEqual([
      { id: "definition-1", name: "Existing" },
      { id: "local:definition-1", name: "New definition" },
    ]);
    expect(await getOfflineState(account.id)).toMatchObject({
      pending: 1,
      blocked: 0,
      preparedAt: expect.any(String),
    });
  });

  it("removes account workspaces and the remembered identity on logout", async () => {
    await rememberOfflineAccount(account);
    await markOfflinePrepared(account.id);
    await enqueueOfflineCommand(
      account.id,
      createOfflineCommand("maintenance-create", { description: "Cleaned" }),
    );

    await clearAllOfflineData();

    expect(getRememberedAccountId()).toBeNull();
    expect(await getOfflineState(account.id)).toMatchObject({
      pending: 0,
      blocked: 0,
      preparedAt: null,
    });
    expect(await hasOfflineData()).toBe(false);
  });
});
