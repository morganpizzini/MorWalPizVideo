import {
  createWorkshopRecipe,
  updateWorkshopRecipe,
} from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" && value.trim() ? value.trim() : null;
};

const numberValue = (form: FormData, key: string) => {
  const value = text(form, key);
  if (value === null) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
};

export async function action({ request }: ActionFunctionArgs) {
  const form = await request.formData();
  const intent = text(form, "intent") ?? "create";
  const name = text(form, "name");
  const caliber = text(form, "caliber");
  const bulletWeight = numberValue(form, "bulletWeight");
  const powderWeight = numberValue(form, "powderWeight");

  if (
    !name ||
    !caliber ||
    bulletWeight === undefined ||
    powderWeight === undefined
  ) {
    return { ok: false as const, error: "Please review the form.", intent };
  }

  const recipe = {
    name,
    caliber,
    oal: text(form, "oal"),
    bulletWeight,
    bulletForm: text(form, "bulletForm"),
    drawing: text(form, "drawing"),
    primer: text(form, "primer"),
    powder: text(form, "powder"),
    powderWeight,
    notes: text(form, "notes"),
  };

  try {
    if (intent === "update") {
      const recipeId = text(form, "recipeId");
      const version = Number(text(form, "version"));
      if (!recipeId || !Number.isInteger(version) || version < 1) {
        return { ok: false as const, error: "Please review the form.", intent };
      }

      await updateWorkshopRecipe(recipeId, {
        ...recipe,
        version,
        idempotencyKey: crypto.randomUUID(),
      });
    } else if (intent === "create") {
      await createWorkshopRecipe(recipe);
    } else {
      return { ok: false as const, error: "Unsupported recipe action.", intent };
    }

    return { ok: true as const, intent };
  } catch (error) {
    return {
      ok: false as const,
      error:
        error instanceof Error
          ? error.message
          : "The request could not be completed.",
      intent,
    };
  }
}
