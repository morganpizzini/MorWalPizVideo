import {
  archiveWorkshopComponentDefinition,
  createAndInstallWorkshopComponent,
  createWorkshopComponentDefinition,
  getWorkshopBatches,
  getWorkshopComponentDefinitions,
  getWorkshopComponentInstallations,
  getWorkshopComponents,
  getWorkshopFirearmDetail,
  getWorkshopFirearms,
  getWorkshopRecipes,
  getWorkshopShootingSessions,
  recordWorkshopMaintenance,
  recordWorkshopShooting,
  removeWorkshopComponent,
} from "@morwalpizvideo/services";
import { ApiResponseError } from "@morwalpizvideo/services";
import type {
  ArchiveWorkshopComponentDefinitionRequest,
  CreateAndInstallWorkshopComponentRequest,
  CreateWorkshopComponentDefinitionRequest,
  CreateWorkshopMaintenanceRequest,
  CreateWorkshopShootingRequest,
  RemoveWorkshopComponentRequest,
} from "@morwalpizvideo/models";
import type { ActionFunctionArgs } from "react-router";
import {
  getOfflineCommands,
  getRememberedAccountId,
  isNetworkFailure,
  markOfflinePrepared,
  removeOfflineCommand,
  resolveOfflineId,
  saveOfflineDataset,
  saveOfflineIdMapping,
  updateOfflineCommand,
  type OfflineCommand,
} from "../services/offlineStore";

function payload<T>(command: OfflineCommand, key: string): T {
  const value = command.payload[key];
  if (!value || typeof value !== "object") throw new Error(`Offline command ${command.id} is missing ${key}.`);
  return value as T;
}

async function resolveReferences<T>(accountId: string, value: T): Promise<T> {
  if (typeof value === "string") return await resolveOfflineId(accountId, value) as T;
  if (Array.isArray(value)) {
    return await Promise.all(value.map(item => resolveReferences(accountId, item))) as T;
  }
  if (value && typeof value === "object") {
    const entries = await Promise.all(Object.entries(value).map(async ([key, item]) =>
      [key, await resolveReferences(accountId, item)] as const));
    return Object.fromEntries(entries) as T;
  }
  return value;
}

async function prepareOfflineWorkspace(accountId: string) {
  let storagePersisted = false;
  if (typeof navigator !== "undefined" && navigator.storage?.persist) {
    try {
      storagePersisted = await navigator.storage.persist();
    } catch (error) {
      console.warn("Workshop persistent storage could not be granted.", error);
    }
  }
  const [firearms, definitions, components, installations, recipes, batches, sessions] = await Promise.all([
    getWorkshopFirearms(),
    getWorkshopComponentDefinitions(),
    getWorkshopComponents(),
    getWorkshopComponentInstallations(),
    getWorkshopRecipes(),
    getWorkshopBatches(),
    getWorkshopShootingSessions(),
  ]);
  const entries = await Promise.all(firearms.map(async firearm => [
    `firearm-detail:${firearm.id}`,
    await getWorkshopFirearmDetail(firearm.id),
  ] as const));
  await Promise.all([
    saveOfflineDataset(accountId, "firearms", firearms),
    saveOfflineDataset(accountId, "definitions", definitions),
    saveOfflineDataset(accountId, "components", components),
    saveOfflineDataset(accountId, "installations", installations),
    saveOfflineDataset(accountId, "recipes", recipes),
    saveOfflineDataset(accountId, "batches", batches),
    saveOfflineDataset(accountId, "sessions", sessions),
    ...entries.map(([key, detail]) => saveOfflineDataset(accountId, key as `firearm-detail:${string}`, detail)),
  ]);
  await markOfflinePrepared(accountId, storagePersisted);
  return { ok: true as const, preparedAt: new Date().toISOString(), storagePersisted };
}

async function synchronizeOfflineCommands(accountId: string) {
  if (typeof navigator !== "undefined" && !navigator.onLine) return { ok: true as const, processed: 0 };
  let processed = 0;
  const commands = await getOfflineCommands(accountId);

  for (const command of commands) {
    if (command.status === "blocked") break;
    try {
      if (command.kind === "definition-create") {
        const request = await resolveReferences(
          accountId,
          payload<CreateWorkshopComponentDefinitionRequest>(command, "request"),
        );
        const created = await createWorkshopComponentDefinition(request);
        const localId = command.localIds?.definition;
        if (localId) await saveOfflineIdMapping(accountId, localId, created.id);
      } else if (command.kind === "definition-archive") {
        const request = await resolveReferences(
          accountId,
          payload<ArchiveWorkshopComponentDefinitionRequest & { definitionId: string }>(command, "request"),
        );
        const { definitionId, ...body } = request;
        await archiveWorkshopComponentDefinition(definitionId, body);
      } else if (command.kind === "component-create") {
        const request = await resolveReferences(
          accountId,
          payload<CreateAndInstallWorkshopComponentRequest>(command, "request"),
        );
        const created = await createAndInstallWorkshopComponent(request);
        if (command.localIds?.component) {
          await saveOfflineIdMapping(accountId, command.localIds.component, created.component.id);
        }
        if (command.localIds?.installation && created.installation?.id) {
          await saveOfflineIdMapping(accountId, command.localIds.installation, created.installation.id);
        }
      } else if (command.kind === "component-swap") {
        const request = await resolveReferences(
          accountId,
          payload<{
            installationId: string;
            removal: RemoveWorkshopComponentRequest;
            replacement: CreateAndInstallWorkshopComponentRequest;
          }>(command, "request"),
        );
        if (command.phase === "remove") {
          await removeWorkshopComponent(request.installationId, request.removal);
          await updateOfflineCommand(accountId, command.id, current => ({ ...current, phase: "create" }));
          continue;
        }
        const created = await createAndInstallWorkshopComponent(request.replacement);
        if (command.localIds?.component) {
          await saveOfflineIdMapping(accountId, command.localIds.component, created.component.id);
        }
        if (command.localIds?.installation && created.installation?.id) {
          await saveOfflineIdMapping(accountId, command.localIds.installation, created.installation.id);
        }
      } else if (command.kind === "maintenance-create") {
        const request = await resolveReferences(
          accountId,
          payload<CreateWorkshopMaintenanceRequest>(command, "request"),
        );
        await recordWorkshopMaintenance(request);
      } else if (command.kind === "shooting-create") {
        const request = await resolveReferences(
          accountId,
          payload<CreateWorkshopShootingRequest>(command, "request"),
        );
        await recordWorkshopShooting(request);
      }
      await removeOfflineCommand(accountId, command.id);
      processed++;
    } catch (error) {
      if (error instanceof ApiResponseError && error.status === 401) {
        return { ok: false as const, authRequired: true as const, processed };
      }
      if (isNetworkFailure(error) ||
          (error instanceof ApiResponseError && (error.status ?? 500) >= 500)) {
        break;
      }
      const partialSwap = command.kind === "component-swap" && command.phase === "create";
      const errorMessage = error instanceof Error ? error.message : "The operation was rejected by the server.";
      await updateOfflineCommand(accountId, command.id, current => ({
        ...current,
        status: "blocked",
        error: partialSwap
          ? `The old component was removed, but the replacement was not installed: ${errorMessage}`
          : `Operation aborted: ${errorMessage}`,
      }));
      break;
    }
  }
  return { ok: true as const, processed };
}

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const intent = form.get("intent");
  const accountId = getRememberedAccountId();
  if (!accountId) return { ok: false as const, error: "Sign in before using offline Workshop data." };
  if (intent === "prepare") {
    try {
      return await prepareOfflineWorkspace(accountId);
    } catch (error) {
      if (isNetworkFailure(error)) {
        return { ok: false as const, error: "Connect to the internet to prepare this device for offline use." };
      }
      return {
        ok: false as const,
        error: error instanceof Error ? error.message : "Offline data could not be saved on this device.",
      };
    }
  }
  if (intent === "sync") return synchronizeOfflineCommands(accountId);
  return { ok: false as const, error: "Unsupported offline action." };
}
