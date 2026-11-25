import {
  archiveWorkshopComponent,
  archiveWorkshopComponentDefinition,
  createAndInstallWorkshopComponent,
  createWorkshopComponentDefinition,
  deleteWorkshopComponent,
  deleteWorkshopComponentDefinition,
  installWorkshopComponent,
  removeWorkshopComponent,
  updateWorkshopComponent,
  updateWorkshopComponentDefinition,
} from "@morwalpizvideo/services";
import type {
  CreateAndInstallWorkshopComponentRequest,
  CreateWorkshopComponentDefinitionRequest,
} from "@morwalpizvideo/models";
import type { ActionFunctionArgs } from "react-router";
import {
  componentSnapshot,
  definitionSnapshot,
  getOfflineDataset,
  getRememberedAccountId,
  installationSnapshot,
} from "../../services/offlineStore";
import { runOrQueueOffline } from "../../services/offlineMutations";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" && value.trim() ? value.trim() : null;
};
const required = (form: FormData, key: string) => text(form, key) ?? "";
const revision = (form: FormData, key = "revision") => Number(form.get(key) ?? 0);
const tags = (form: FormData) =>
  required(form, "compatibilityTags")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);
const nonNegativeNumber = (form: FormData, key: string, fallback = 0) => {
  const raw = form.get(key);
  if (raw === null || raw === "") return fallback;
  const value = Number(raw);
  if (!Number.isSafeInteger(value) || value < 0) throw new Error(`${key} must be a non-negative integer.`);
  return value;
};
const optionalNonNegativeNumber = (form: FormData, key: string) => {
  const raw = form.get(key);
  if (raw === null || raw === "") return null;
  return nonNegativeNumber(form, key);
};

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const intent = required(form, "intent");
  const id = required(form, "id");
  const idempotencyKey = crypto.randomUUID();

  switch (intent) {
    case "create-definition": {
      const request: CreateWorkshopComponentDefinitionRequest = {
        name: required(form, "name"),
        description: text(form, "description"),
        notes: text(form, "notes"),
        idempotencyKey,
      };
      const localDefinitionId = `local:${crypto.randomUUID()}`;
      const accountId = getRememberedAccountId() ?? "";
      const result = await runOrQueueOffline(
        () => createWorkshopComponentDefinition(request),
        "definition-create",
        { request },
        accountId
          ? { definitions: { upsert: [definitionSnapshot(accountId, localDefinitionId, request)] } }
          : undefined,
        { definition: localDefinitionId },
      );
      if (result.offline) return { ok: true as const, intent, offline: true };
      break;
    }
    case "update-definition":
      await updateWorkshopComponentDefinition(id, {
        name: required(form, "name"),
        description: text(form, "description"),
        notes: text(form, "notes"),
        revision: revision(form),
        idempotencyKey,
      });
      break;
    case "archive-definition": {
      const request = { definitionId: id, revision: revision(form), idempotencyKey };
      const accountId = getRememberedAccountId() ?? "";
      const definition = accountId
        ? getOfflineDataset<import("@morwalpizvideo/models").WorkshopComponentDefinition[]>(accountId, "definitions")
          .then(items => items?.find(item => item.id === id))
        : Promise.resolve(undefined);
      const item = await definition;
      const result = await runOrQueueOffline(
        () => archiveWorkshopComponentDefinition(id, { revision: request.revision, idempotencyKey }),
        "definition-archive",
        { request },
        item
          ? { definitions: { upsert: [{ ...item, archived: true, revision: item.revision + 1 }] } }
          : undefined,
      );
      if (result.offline) return { ok: true as const, intent, offline: true };
      break;
    }
    case "delete-definition":
      await deleteWorkshopComponentDefinition(id, { revision: revision(form), idempotencyKey });
      break;
    case "create-component": {
      const request: CreateAndInstallWorkshopComponentRequest = {
        definitionId: required(form, "definitionId"),
        firearmId: text(form, "firearmId"),
        installedAt: new Date().toISOString(),
        identificationNote: text(form, "identificationNote"),
        manufacturer: text(form, "manufacturer"),
        model: text(form, "model"),
        serviceInterval: text(form, "serviceInterval") ? Number(form.get("serviceInterval")) : null,
        trackingMode: required(form, "trackingMode") as "Counter" | "Interval" | "Manual",
        compatibilityTags: tags(form),
        notes: text(form, "notes"),
        initialRounds: nonNegativeNumber(form, "initialRounds"),
        idempotencyKey,
      };
      const accountId = getRememberedAccountId() ?? "";
      const localComponentId = `local:${crypto.randomUUID()}`;
      const localInstallationId = `local:${crypto.randomUUID()}`;
      const component = accountId ? componentSnapshot(accountId, localComponentId, request) : null;
      const installation = accountId && request.firearmId
        ? installationSnapshot(accountId, localInstallationId, localComponentId, request.firearmId, request.installedAt)
        : null;
      const replacementInstallationId = text(form, "replacementInstallationId");
      if (replacementInstallationId) {
        const removal = {
          removedAt: request.installedAt,
          removalNotes: null,
          revision: revision(form, "replacementInstallationRevision"),
          idempotencyKey: `${idempotencyKey}:remove`,
        };
        const swapRequest = {
          installationId: replacementInstallationId,
          removal,
          replacement: request,
        };
        const existingInstallation = JSON.parse(text(form, "replacementInstallation") || "null") as
          | { id: string; accountId: string; componentId: string; firearmId: string; installedAt: string; revision: number }
          | null;
        const result = await runOrQueueOffline(
          async () => {
            await removeWorkshopComponent(replacementInstallationId, removal);
            return createAndInstallWorkshopComponent(request);
          },
          "component-swap",
          { request: swapRequest },
          accountId && component && installation && existingInstallation
            ? {
                components: { upsert: [component] },
                installations: {
                  upsert: [
                    { ...existingInstallation, removedAt: request.installedAt },
                    installation,
                  ],
                },
              }
            : undefined,
          { component: localComponentId, installation: localInstallationId },
        );
        if (result.offline) return { ok: true as const, intent, offline: true };
        return { ok: true as const, intent, partial: false };
      }
      const result = await runOrQueueOffline(
        () => createAndInstallWorkshopComponent(request),
        "component-create",
        { request },
        accountId && component
          ? {
              components: { upsert: [component] },
              ...(installation ? { installations: { upsert: [installation] } } : {}),
            }
          : undefined,
        { component: localComponentId, installation: localInstallationId },
      );
      if (result.offline) return { ok: true as const, intent, offline: true };
      break;
    }
    case "update-component":
      await updateWorkshopComponent(id, {
        definitionId: required(form, "definitionId"),
        identificationNote: text(form, "identificationNote"),
        manufacturer: text(form, "manufacturer"),
        model: text(form, "model"),
        serviceInterval: text(form, "serviceInterval") ? Number(form.get("serviceInterval")) : null,
        trackingMode: required(form, "trackingMode") as "Counter" | "Interval" | "Manual",
        compatibilityTags: tags(form),
        notes: text(form, "notes"),
        totalRounds: optionalNonNegativeNumber(form, "totalRounds"),
        revision: revision(form),
        idempotencyKey,
      });
      {
        const existingFirearmId = text(form, "existingFirearmId");
        const firearmId = text(form, "firearmId");
        if (existingFirearmId !== firearmId) {
          const installationId = text(form, "installationId");
          if (installationId) {
            await removeWorkshopComponent(installationId, {
              removedAt: new Date().toISOString(),
              revision: revision(form, "installationRevision"),
              idempotencyKey: crypto.randomUUID(),
            });
          }
          if (firearmId) {
            await installWorkshopComponent({
              componentId: id,
              firearmId,
              installedAt: new Date().toISOString(),
              idempotencyKey: crypto.randomUUID(),
            });
          }
        }
      }
      break;
    case "archive-component":
      await archiveWorkshopComponent(id, { revision: revision(form), idempotencyKey });
      break;
    case "delete-component":
      await deleteWorkshopComponent(id, { revision: revision(form), idempotencyKey });
      break;
    default:
      throw new Error("Unsupported component action.");
  }
  return { ok: true, intent };
}
