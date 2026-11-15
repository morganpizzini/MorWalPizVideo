import {
  archiveWorkshopFirearm,
  recordWorkshopMaintenance,
  updateWorkshopFirearm,
} from "@morwalpizvideo/services";
import { redirect, type ActionFunctionArgs } from "react-router";

const value = (form: FormData, key: string) => {
  const result = form.get(key);
  return typeof result === "string" && result.trim() ? result.trim() : null;
};

export async function action({ request, params }: ActionFunctionArgs) {
  const firearmId = params.id;
  if (!firearmId) throw new Response("Not found", { status: 404 });
  const form = await request.formData();
  const intent = value(form, "intent");
  const idempotencyKey = crypto.randomUUID();
  if (intent === "maintenance") {
    return recordWorkshopMaintenance({
      firearmId,
      eventType: (value(form, "eventType") ?? "General") as "Inspection",
      date: value(form, "date") ?? new Date().toISOString(),
      roundCountAtEvent: null,
      description: value(form, "description") ?? "",
      notes: value(form, "notes"),
      idempotencyKey,
    });
  }
  if (intent === "shooting") {
    return redirect(`/sessions/shooting?firearmId=${encodeURIComponent(firearmId)}`);
  }
  if (intent === "archive") {
    return archiveWorkshopFirearm(firearmId, {
      revision: Number(value(form, "revision") ?? 0),
      idempotencyKey,
    });
  }
  if (intent === "edit") {
    return updateWorkshopFirearm(firearmId, {
      manufacturer: value(form, "manufacturer") ?? "",
      model: value(form, "model") ?? "",
      caliber: value(form, "caliber") ?? "",
      category: value(form, "category"),
      nickname: value(form, "nickname"),
      notes: value(form, "notes"),
      revision: Number(value(form, "revision") ?? 0),
      idempotencyKey,
    });
  }
  throw new Error("Unsupported firearm detail action.");
}
