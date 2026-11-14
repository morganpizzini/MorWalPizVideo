import {
  archiveWorkshopComponent,
  archiveWorkshopComponentDefinition,
  createAndInstallWorkshopComponent,
  createWorkshopComponentDefinition,
  deleteWorkshopComponent,
  deleteWorkshopComponentDefinition,
  updateWorkshopComponent,
  updateWorkshopComponentDefinition,
} from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" && value.trim() ? value.trim() : null;
};
const required = (form: FormData, key: string) => text(form, key) ?? "";
const revision = (form: FormData) => Number(form.get("revision") ?? 0);
const tags = (form: FormData) =>
  required(form, "compatibilityTags")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);

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
        revision: revision(form),
        idempotencyKey,
      });
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
