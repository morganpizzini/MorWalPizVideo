import {
  archiveWorkshopComponent,
  archiveWorkshopComponentDefinition,
  createAndInstallWorkshopComponent,
  createWorkshopComponentDefinition,
  deleteWorkshopComponent,
  deleteWorkshopComponentDefinition,
  installWorkshopComponent,
  recordWorkshopComponentPurchase,
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
      const quantityValue = form.get("componentQuantity");
      const quantity = quantityValue === null || quantityValue === "" ? 1 : Number(quantityValue);
      if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > 100)
        return { ok: false as const, intent, error: "Enter a quantity from 1 to 100." };
      const priceValue = form.get("unitPrice");
      const priceText = typeof priceValue === "string" ? priceValue.trim() : "";
      const unitPrice = priceText ? Number(priceText) : null;
      if (priceText && (!/^\d+(?:\.\d{1,2})?$/.test(priceText) || !Number.isFinite(unitPrice) || unitPrice! < 0))
        return { ok: false as const, intent, error: "Enter a valid non-negative price with up to two decimal places." };
      const purchaseDate = text(form, "purchaseDate");
      const currencyValue = text(form, "currency");
      if (unitPrice !== null && (!purchaseDate || !/^\d{4}-\d{2}-\d{2}$/.test(purchaseDate) ||
          Number.isNaN(Date.parse(`${purchaseDate}T00:00:00Z`)) || !currencyValue || !/^[A-Za-z]{3}$/.test(currencyValue)))
        return { ok: false as const, intent, error: "Enter a valid purchase date and three-letter currency code." };
      if (quantity > 1 && (text(form, "firearmId") || text(form, "replacementInstallationId")))
        return { ok: false as const, intent, error: "Bulk creation adds uninstalled spare components. Create one component to install or replace it." };
      const needsOnline = quantity > 1 || unitPrice !== null;
      if (needsOnline && typeof navigator !== "undefined" && !navigator.onLine)
        return { ok: false as const, intent, error: "Bulk creation and purchase prices require an online connection." };

      const creationKey = required(form, "creationIdempotencyKey") || idempotencyKey;
      const replacementInstallationId = text(form, "replacementInstallationId");
      const accountId = getRememberedAccountId() ?? "";
      const installedAt = new Date().toISOString();
      let completedCount = 0;
      let mutationStarted = false;
      try {
        for (let index = 0; index < quantity; index++) {
          const itemKey = `${creationKey}:${index + 1}`;
          const request: CreateAndInstallWorkshopComponentRequest = {
            definitionId: required(form, "definitionId"),
            firearmId: text(form, "firearmId"),
            installedAt,
            identificationNote: text(form, "identificationNote"),
            manufacturer: text(form, "manufacturer"),
            model: text(form, "model"),
            serviceInterval: text(form, "serviceInterval") ? Number(form.get("serviceInterval")) : null,
            trackingMode: required(form, "trackingMode") as "Counter" | "Interval" | "Manual",
            compatibilityTags: tags(form),
            notes: text(form, "notes"),
            initialRounds: nonNegativeNumber(form, "initialRounds"),
            idempotencyKey: itemKey,
          };

          if (!needsOnline) {
            const localComponentId = `local:${crypto.randomUUID()}`;
            const localInstallationId = `local:${crypto.randomUUID()}`;
            const component = accountId ? componentSnapshot(accountId, localComponentId, request) : null;
            const installation = accountId && request.firearmId
              ? installationSnapshot(accountId, localInstallationId, localComponentId, request.firearmId, installedAt)
              : null;
            if (replacementInstallationId) {
              const removal = {
                removedAt: installedAt,
                removalNotes: null,
                revision: revision(form, "replacementInstallationRevision"),
                idempotencyKey: `${itemKey}:remove`,
              };
              const existingInstallation = JSON.parse(text(form, "replacementInstallation") || "null") as
                | { id: string; accountId: string; componentId: string; firearmId: string; installedAt: string; revision: number }
                | null;
              const result = await runOrQueueOffline(
                async () => {
                  await removeWorkshopComponent(replacementInstallationId, removal);
                  mutationStarted = true;
                  return createAndInstallWorkshopComponent(request);
                },
                "component-swap",
                { request: { installationId: replacementInstallationId, removal, replacement: request } },
                accountId && component && installation && existingInstallation
                  ? {
                      components: { upsert: [component] },
                      installations: {
                        upsert: [
                          { ...existingInstallation, removedAt: installedAt },
                          installation,
                        ],
                      },
                    }
                  : undefined,
                { component: localComponentId, installation: localInstallationId },
              );
              if (result.offline) return { ok: true as const, intent, offline: true };
            } else {
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
            }
            mutationStarted = true;
            completedCount++;
            continue;
          }

          if (replacementInstallationId) {
            await removeWorkshopComponent(replacementInstallationId, {
              removedAt: installedAt,
              removalNotes: null,
              revision: revision(form, "replacementInstallationRevision"),
              idempotencyKey: `${itemKey}:remove`,
            });
            mutationStarted = true;
          }
          const created = await createAndInstallWorkshopComponent(request);
          mutationStarted = true;
          if (unitPrice !== null) {
            await recordWorkshopComponentPurchase(created.component.id, {
              purchaseDate: purchaseDate!,
              totalPrice: unitPrice,
              currency: currencyValue!.toUpperCase(),
              idempotencyKey: `${itemKey}:purchase`,
            });
          }
          completedCount++;
        }
      } catch (error) {
        const detail = error instanceof Error ? error.message : "The request could not be completed.";
        const progress = mutationStarted
          ? `Created or changed ${completedCount} of ${quantity} components.`
          : "No component creation was confirmed.";
        return {
          ok: false as const,
          intent,
          partial: mutationStarted,
          error: mutationStarted
            ? "Component batch creation was interrupted. Retry without changing the form to resume safely."
            : "Component creation failed.",
          details: `${progress} ${detail}`,
        };
      }
      return { ok: true as const, intent, createdCount: completedCount };
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
