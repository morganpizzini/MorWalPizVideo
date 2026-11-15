import { createWorkshopFirearm } from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const manufacturer = text(form, "manufacturer");
  const model = text(form, "model");
  const caliber = text(form, "caliber");
  const initialRounds = Number(text(form, "initialRounds") || 0);
  if (!manufacturer || !model || !caliber || !Number.isInteger(initialRounds) || initialRounds < 0) {
    return { ok: false as const, error: "Please review the firearm form." };
  }

  try {
    await createWorkshopFirearm({
      manufacturer,
      model,
      caliber,
      initialRounds,
      nickname: text(form, "nickname") || null,
      category: text(form, "category") || null,
      notes: text(form, "notes") || null,
      idempotencyKey: text(form, "idempotencyKey") || crypto.randomUUID(),
    });
    return { ok: true as const };
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "The request could not be completed.",
    };
  }
}
