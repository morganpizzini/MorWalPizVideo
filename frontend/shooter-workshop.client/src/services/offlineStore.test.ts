import { beforeEach, describe, expect, it } from "vitest";
import {
  clearAllOfflineData,
  createOfflineCommand,
  enqueueOfflineCommand,
  getOfflineDataset,
  getOfflineCommands,
  getOfflineState,
  getRememberedAccountId,
  hasOfflineData,
  markOfflinePrepared,
  rememberOfflineAccount,
  resolveOfflineId,
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

  it("removes legacy Drawing values from cached recipe and batch snapshots", async () => {
    await rememberOfflineAccount(account);

    await Promise.all([
      saveOfflineDataset(account.id, "recipes", [
        { id: "recipe-1", name: "Practice", bulletForm: "FMJ", drawing: "legacy" },
      ]),
      saveOfflineDataset(account.id, "batches", [
        {
          id: "batch-1",
          recipeSnapshot: { name: "Practice", bulletForm: "FMJ", drawing: "legacy" },
        },
      ]),
    ]);

    expect(await getOfflineDataset(account.id, "recipes")).toEqual([
      { id: "recipe-1", name: "Practice", bulletForm: "FMJ" },
    ]);
    expect(await getOfflineDataset(account.id, "batches")).toEqual([
      {
        id: "batch-1",
        recipeSnapshot: { name: "Practice", bulletForm: "FMJ" },
      },
    ]);
  });

  it("migrates existing snapshots without changing other workspace data or commands", async () => {
    const databaseName = "shooter-workshop-offline";
    await new Promise<void>((resolve, reject) => {
      const request = indexedDB.deleteDatabase(databaseName);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
      request.onblocked = () => reject(new Error("Legacy database deletion was blocked."));
    });

    const legacy = {
      accountId: account.id,
      account,
      datasets: {
        firearms: [{ id: "firearm-1", manufacturer: "Maker" }],
        recipes: [
          { id: "recipe-1", name: "Practice", bulletForm: "FMJ", drawing: "legacy" },
        ],
        batches: [
          {
            id: "batch-1",
            recipeSnapshot: { name: "Practice", bulletForm: "FMJ", drawing: "legacy" },
          },
        ],
      },
      commands: [
        {
          id: "pending-1",
          kind: "maintenance-create",
          payload: { description: "Queued", drawing: "keep pending command intact" },
          createdAt: "2026-11-25T00:00:00Z",
          status: "pending",
        },
      ],
      idMappings: { "local:definition-1": "definition-1" },
      preparedAt: "2026-11-24T00:00:00Z",
      storagePersisted: true,
    };
    const oldDatabase = await new Promise<IDBDatabase>((resolve, reject) => {
      const request = indexedDB.open(databaseName, 1);
      request.onupgradeneeded = () => {
        request.result.createObjectStore("workspaces", { keyPath: "accountId" });
      };
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    await new Promise<void>((resolve, reject) => {
      const transaction = oldDatabase.transaction("workspaces", "readwrite");
      transaction.objectStore("workspaces").put(legacy);
      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
      transaction.onabort = () => reject(transaction.error);
    });
    oldDatabase.close();

    expect(await getOfflineDataset(account.id, "recipes")).toEqual([
      { id: "recipe-1", name: "Practice", bulletForm: "FMJ" },
    ]);
    expect(await getOfflineDataset(account.id, "batches")).toEqual([
      {
        id: "batch-1",
        recipeSnapshot: { name: "Practice", bulletForm: "FMJ" },
      },
    ]);
    expect(await getOfflineDataset(account.id, "firearms")).toEqual([
      { id: "firearm-1", manufacturer: "Maker" },
    ]);
    expect(await getOfflineCommands(account.id)).toEqual(legacy.commands);
    expect(await resolveOfflineId(account.id, "local:definition-1")).toBe("definition-1");
    expect(await getOfflineState(account.id)).toMatchObject({
      pending: 1,
      preparedAt: legacy.preparedAt,
      storagePersisted: true,
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
