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
import type { ActionFunctionArgs } from "react-router";

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
    case "create-definition":
      await createWorkshopComponentDefinition({
        name: required(form, "name"),
        description: text(form, "description"),
        notes: text(form, "notes"),
      });
      break;
    case "update-definition":
      await updateWorkshopComponentDefinition(id, {
        name: required(form, "name"),
        description: text(form, "description"),
        notes: text(form, "notes"),
        revision: revision(form),
        idempotencyKey,
      });
      break;
    case "archive-definition":
      await archiveWorkshopComponentDefinition(id, { revision: revision(form), idempotencyKey });
      break;
    case "delete-definition":
      await deleteWorkshopComponentDefinition(id, { revision: revision(form), idempotencyKey });
      break;
    case "create-component":
      await createAndInstallWorkshopComponent({
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
      });
      break;
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
