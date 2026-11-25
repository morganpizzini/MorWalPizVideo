import { recordWorkshopMaintenance } from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";
import { getRememberedAccountId, maintenanceSnapshot } from "../../services/offlineStore";
import { runOrQueueOffline } from "../../services/offlineMutations";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const date = text(form, "date");
  const description = text(form, "description");
  const targetType = text(form, "targetType");
  const targetId = text(form, "targetId");
  const idempotencyKey = text(form, "idempotencyKey");
  if (!date || !description || !targetId || (targetType !== "firearm" && targetType !== "component")) {
    return { ok: false as const, error: "Please review the maintenance form." };
  }

  try {
    const request = {
      firearmId: targetType === "firearm" ? targetId : null,
      componentId: targetType === "component" ? targetId : null,
      eventType: text(form, "eventType") as "Inspection",
      date: new Date(date).toISOString(),
      roundCountAtEvent: null,
      description,
      notes: text(form, "notes") || null,
      idempotencyKey: idempotencyKey || crypto.randomUUID(),
    };
    const accountId = getRememberedAccountId() ?? "";
    const localEventId = `local:${crypto.randomUUID()}`;
    const result = await runOrQueueOffline(
      () => recordWorkshopMaintenance(request),
      "maintenance-create",
      { request },
      accountId
        ? { maintenance: { upsert: [maintenanceSnapshot(accountId, localEventId, request)] } }
        : undefined,
    );
    if (result.offline) return { ok: true as const, offline: true };
    return { ok: true as const };
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "The request could not be completed.",
    };
  }
}
