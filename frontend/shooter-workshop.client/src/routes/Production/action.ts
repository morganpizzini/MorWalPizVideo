import { recordWorkshopProduction } from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const recipeId = text(form, "recipeId");
  const productionDate = text(form, "productionDate");
  const quantityProduced = Number(text(form, "quantityProduced"));
  const notes = text(form, "notes");

  if (!recipeId || !productionDate || !Number.isInteger(quantityProduced) || quantityProduced < 1) {
    return { ok: false as const, error: "Please review the form." };
  }

  try {
    await recordWorkshopProduction({
      recipeId,
      productionDate,
      quantityProduced,
      notes: notes || null,
      idempotencyKey: crypto.randomUUID(),
    });
    return { ok: true as const };
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "The request could not be completed.",
    };
  }
}
