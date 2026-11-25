import type {
  CreateAndInstallWorkshopComponentRequest,
  CreateWorkshopMaintenanceRequest,
  CreateWorkshopShootingRequest,
  CreateWorkshopComponentDefinitionRequest,
  WorkshopComponentDefinition,
  WorkshopPhysicalComponent,
  WorkshopComponentInstallation,
  WorkshopMaintenanceEvent,
  WorkshopShootingSessionRecord,
  PublicAccountSummary,
} from "@morwalpizvideo/models";

export type OfflineDataset =
  | "firearms"
  | "definitions"
  | "components"
  | "installations"
  | "recipes"
  | "batches"
  | "sessions"
  | "maintenance"
  | `firearm-detail:${string}`;

export type OfflineCommandKind =
  | "definition-create"
  | "definition-archive"
  | "component-create"
  | "component-swap"
  | "maintenance-create"
  | "shooting-create";

export interface OfflineCommand {
  id: string;
  kind: OfflineCommandKind;
  payload: Record<string, unknown>;
  createdAt: string;
  status: "pending" | "blocked";
  error?: string;
  phase?: "remove" | "create";
  localIds?: Record<string, string>;
  changes?: Partial<Record<OfflineDataset, {
    upsert?: unknown[];
    removeIds?: string[];
  }>>;
}

interface WorkshopWorkspace {
  accountId: string;
  account: PublicAccountSummary;
  datasets: Partial<Record<OfflineDataset, unknown>>;
  commands: OfflineCommand[];
  idMappings: Record<string, string>;
  preparedAt: string | null;
  storagePersisted: boolean;
}

const databaseName = "shooter-workshop-offline";
const workspaceStore = "workspaces";
const rememberedAccountKey = "shooter-workshop.offline-account-id";
const signedOutKey = "shooter-workshop.offline-signed-out";
const changeEventName = "shooter-workshop-offline-change";

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const opening = indexedDB.open(databaseName, 1);
    opening.onupgradeneeded = () => {
      if (!opening.result.objectStoreNames.contains(workspaceStore)) {
        opening.result.createObjectStore(workspaceStore, { keyPath: "accountId" });
      }
    };
    opening.onsuccess = () => resolve(opening.result);
    opening.onerror = () => reject(opening.error ?? new Error("Offline storage could not be opened."));
    opening.onblocked = () => reject(new Error("Offline storage upgrade is blocked by another tab."));
  });
}

async function transact<T>(
  mode: IDBTransactionMode,
  operation: (store: IDBObjectStore) => IDBRequest<T>,
): Promise<T> {
  const database = await openDatabase();
  return new Promise<T>((resolve, reject) => {
    let result: T;
    let hasResult = false;
    const transaction = database.transaction(workspaceStore, mode);
    const close = () => database.close();
    const request = operation(transaction.objectStore(workspaceStore));
    request.onsuccess = () => {
      result = request.result;
      hasResult = true;
    };
    request.onerror = () => {
      close();
      reject(request.error ?? new Error("Offline storage operation failed."));
    };
    transaction.onabort = () => {
      close();
      reject(transaction.error ?? new Error("Offline storage transaction was aborted."));
    };
    transaction.onerror = () => {
      close();
      reject(transaction.error ?? new Error("Offline storage transaction failed."));
    };
    transaction.oncomplete = () => {
      close();
      if (!hasResult) {
        reject(new Error("Offline storage transaction completed without a result."));
        return;
      }
      resolve(result);
    };
  });
}

function emitChange(): void {
  if (typeof window !== "undefined") window.dispatchEvent(new Event(changeEventName));
}

function createWorkspace(account: PublicAccountSummary): WorkshopWorkspace {
  return {
    accountId: account.id,
    account,
    datasets: {},
    commands: [],
    idMappings: {},
    preparedAt: null,
    storagePersisted: false,
  };
}

function workspace(accountId: string, account?: PublicAccountSummary): Promise<WorkshopWorkspace | undefined> {
  return transact<WorkshopWorkspace | undefined>("readonly", store => store.get(accountId))
    .then(existing => {
      if (existing || !account) return existing;
      return createWorkspace(account);
    });
}

async function updateWorkspace(
  accountId: string,
  update: (current: WorkshopWorkspace) => WorkshopWorkspace,
): Promise<WorkshopWorkspace> {
  const database = await openDatabase();
  return new Promise((resolve, reject) => {
    const transaction = database.transaction(workspaceStore, "readwrite");
    const store = transaction.objectStore(workspaceStore);
    let next: WorkshopWorkspace | undefined;
    let failure: Error | undefined;
    const request = store.get(accountId);
    request.onsuccess = () => {
      if (!request.result) {
        failure = new Error("Offline account data is unavailable. Reconnect and prepare this device first.");
        transaction.abort();
        return;
      }
      next = update(request.result as WorkshopWorkspace);
      store.put(next);
    };
    transaction.oncomplete = () => {
      database.close();
      if (!next) {
        reject(new Error("Offline account update completed without a result."));
        return;
      }
      emitChange();
      resolve(next);
    };
    transaction.onerror = () => {
      database.close();
      reject(failure ?? transaction.error ?? new Error("Offline account update failed."));
    };
    transaction.onabort = () => {
      database.close();
      reject(failure ?? transaction.error ?? new Error("Offline account update was aborted."));
    };
  });
}

export function getRememberedAccountId(): string | null {
  if (typeof localStorage === "undefined") return null;
  try {
    return localStorage.getItem(rememberedAccountKey);
  } catch (error) {
    console.warn("Workshop offline account could not be read.", error);
    return null;
  }
}

export function isOfflineSignedOut(): boolean {
  if (typeof localStorage === "undefined") return false;
  try {
    return localStorage.getItem(signedOutKey) === "true";
  } catch (error) {
    console.warn("Workshop offline logout state could not be read.", error);
    return false;
  }
}

export async function rememberOfflineAccount(account: PublicAccountSummary): Promise<void> {
  try {
    localStorage.setItem(rememberedAccountKey, account.id);
    localStorage.removeItem(signedOutKey);
  } catch (error) {
    const detail = error instanceof Error ? ` ${error.message}` : "";
    throw new Error(`This browser cannot remember the account for offline use.${detail}`);
  }
  const database = await openDatabase();
  await new Promise<void>((resolve, reject) => {
    const transaction = database.transaction(workspaceStore, "readwrite");
    const store = transaction.objectStore(workspaceStore);
    const request = store.get(account.id);
    request.onsuccess = () => {
      const current = request.result as WorkshopWorkspace | undefined;
      store.put({ ...(current ?? createWorkspace(account)), account });
    };
    transaction.oncomplete = () => {
      database.close();
      emitChange();
      resolve();
    };
    transaction.onerror = () => {
      database.close();
      reject(transaction.error ?? new Error("Offline account storage could not be initialized."));
    };
    transaction.onabort = () => {
      database.close();
      reject(transaction.error ?? new Error("Offline account storage update was aborted."));
    };
  });
}

export async function getRememberedOfflineAccount(): Promise<PublicAccountSummary | null> {
  if (isOfflineSignedOut()) return null;
  const accountId = getRememberedAccountId();
  if (!accountId) return null;
  return (await workspace(accountId))?.account ?? null;
}

export async function saveOfflineDataset(
  accountId: string,
  dataset: OfflineDataset,
  value: unknown,
): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    datasets: { ...current.datasets, [dataset]: value },
  }));
}

export async function markOfflinePrepared(accountId: string, storagePersisted = false): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    preparedAt: new Date().toISOString(),
    storagePersisted,
  }));
}

function applyChanges<T>(base: T, commands: OfflineCommand[], dataset: OfflineDataset): T {
  if (!Array.isArray(base)) return base;
  const records = [...base] as Array<Record<string, unknown>>;
  for (const command of commands) {
    if (command.status !== "pending") continue;
    const change = command.changes?.[dataset];
    if (!change) continue;
    if (change.removeIds?.length) {
      for (let index = records.length - 1; index >= 0; index--) {
        if (change.removeIds.includes(String(records[index].id))) records.splice(index, 1);
      }
    }
    for (const raw of change.upsert ?? []) {
      const record = raw as Record<string, unknown>;
      const index = records.findIndex(item => item.id === record.id);
      if (index >= 0) records[index] = { ...records[index], ...record };
      else records.push(record);
    }
  }
  return records as T;
}

export async function getOfflineDataset<T>(accountId: string, dataset: OfflineDataset): Promise<T | null> {
  const current = await workspace(accountId);
  if (!current || !Object.prototype.hasOwnProperty.call(current.datasets, dataset)) return null;
  return applyChanges(current.datasets[dataset] as T, current.commands, dataset);
}

export async function enqueueOfflineCommand(accountId: string, command: OfflineCommand): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    commands: [...current.commands, command],
  }));
}

export async function getOfflineCommands(accountId: string): Promise<OfflineCommand[]> {
  return (await workspace(accountId))?.commands ?? [];
}

export async function getOfflineState(accountId: string): Promise<{
  pending: number;
  blocked: number;
  preparedAt: string | null;
  storagePersisted: boolean;
  commands: OfflineCommand[];
}> {
  const current = await workspace(accountId);
  const commands = current?.commands ?? [];
  return {
    pending: commands.filter(command => command.status === "pending").length,
    blocked: commands.filter(command => command.status === "blocked").length,
    preparedAt: current?.preparedAt ?? null,
    storagePersisted: current?.storagePersisted ?? false,
    commands,
  };
}

export async function hasOfflineData(): Promise<boolean> {
  const workspaces = await transact<WorkshopWorkspace[]>("readonly", store => store.getAll());
  return workspaces.some(workspace =>
    workspace.preparedAt !== null ||
    workspace.commands.length > 0 ||
    Object.keys(workspace.datasets).length > 0,
  );
}

export async function updateOfflineCommand(
  accountId: string,
  commandId: string,
  update: (command: OfflineCommand) => OfflineCommand,
): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    commands: current.commands.map(command => command.id === commandId ? update(command) : command),
  }));
}

export async function removeOfflineCommand(accountId: string, commandId: string): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    commands: current.commands.filter(command => command.id !== commandId),
  }));
}

export async function saveOfflineIdMapping(accountId: string, localId: string, serverId: string): Promise<void> {
  await updateWorkspace(accountId, current => ({
    ...current,
    idMappings: { ...current.idMappings, [localId]: serverId },
  }));
}

export async function resolveOfflineId(accountId: string, id: string): Promise<string> {
  const mappings = (await workspace(accountId))?.idMappings ?? {};
  return mappings[id] ?? id;
}

export async function clearAllOfflineData(): Promise<void> {
  await transact<undefined>("readwrite", store => store.clear());
  try {
    localStorage.removeItem(rememberedAccountKey);
    localStorage.setItem(signedOutKey, "true");
  } catch (error) {
    console.error("Workshop offline logout marker could not be saved.", error);
    throw error;
  }
  emitChange();
}

export async function clearOfflineSignedOutMarker(): Promise<void> {
  try {
    localStorage.removeItem(signedOutKey);
  } catch (error) {
    console.warn("Workshop offline logout marker could not be cleared.", error);
  }
}

export function subscribeToOfflineChanges(listener: () => void): () => void {
  if (typeof window === "undefined") return () => undefined;
  window.addEventListener(changeEventName, listener);
  return () => window.removeEventListener(changeEventName, listener);
}

export function isNetworkFailure(error: unknown): boolean {
  return error instanceof TypeError ||
    (error instanceof Error && error.name === "NetworkError");
}

export function createOfflineCommand(
  kind: OfflineCommandKind,
  payload: object,
  changes?: OfflineCommand["changes"],
  localIds?: Record<string, string>,
): OfflineCommand {
  return {
    id: crypto.randomUUID(),
    kind,
    payload: payload as Record<string, unknown>,
    createdAt: new Date().toISOString(),
    status: "pending",
    changes,
    localIds,
    ...(kind === "component-swap" ? { phase: "remove" as const } : {}),
  };
}

export function definitionSnapshot(
  accountId: string,
  id: string,
  request: CreateWorkshopComponentDefinitionRequest,
): WorkshopComponentDefinition {
  return {
    id,
    accountId,
    name: request.name,
    description: request.description,
    notes: request.notes,
    manufacturer: request.manufacturer,
    model: request.model,
    defaultServiceInterval: request.defaultServiceInterval,
    trackingMode: request.trackingMode ?? "Counter",
    compatibilityTags: request.compatibilityTags ?? [],
    archived: false,
    revision: 0,
  };
}

export function componentSnapshot(
  accountId: string,
  id: string,
  request: CreateAndInstallWorkshopComponentRequest,
): WorkshopPhysicalComponent {
  return {
    id,
    accountId,
    definitionId: request.definitionId,
    identificationNote: request.identificationNote,
    manufacturer: request.manufacturer,
    model: request.model,
    serviceInterval: request.serviceInterval,
    trackingMode: request.trackingMode,
    compatibilityTags: request.compatibilityTags ?? [],
    totalRounds: request.initialRounds ?? 0,
    status: request.firearmId ? "Installed" : "Inventory",
    notes: request.notes,
    revision: 0,
    archived: false,
  };
}

export function installationSnapshot(
  accountId: string,
  id: string,
  componentId: string,
  firearmId: string,
  installedAt: string,
): WorkshopComponentInstallation {
  return {
    id,
    accountId,
    componentId,
    firearmId,
    installedAt,
    revision: 0,
  };
}

export function shootingSnapshot(
  accountId: string,
  id: string,
  request: CreateWorkshopShootingRequest,
): WorkshopShootingSessionRecord {
  return {
    session: {
      id,
      accountId,
      firearmId: request.firearmId,
      date: request.date,
      totalRounds: request.totalRounds,
      notes: request.notes,
      consumptionRecords: request.ammunitionRecords.map(item => ({
        recipeId: item.recipeId,
        quantity: item.quantity,
      })),
      usageSegments: request.usageSegments,
      untrackedAmmunition: request.untrackedAmmunition,
    },
    superseded: false,
  };
}

export function maintenanceSnapshot(
  accountId: string,
  id: string,
  request: CreateWorkshopMaintenanceRequest,
): WorkshopMaintenanceEvent {
  return {
    id,
    accountId,
    firearmId: request.firearmId,
    componentId: request.componentId,
    eventType: request.eventType,
    date: request.date,
    roundCountAtEvent: request.roundCountAtEvent,
    description: request.description,
    notes: request.notes,
  };
}
