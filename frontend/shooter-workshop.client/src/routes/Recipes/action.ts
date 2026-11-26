import {
  createWorkshopRecipe,
  getWorkshopInventory,
  updateWorkshopRecipe,
} from "@morwalpizvideo/services";
import type { WorkshopInventory } from "@morwalpizvideo/models";
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
  const primerProductId = text(form, "primerProductId");
  const powderProductId = text(form, "powderProductId");

  if (
    !name ||
    !caliber ||
    bulletWeight === undefined ||
    powderWeight === undefined ||
    !primerProductId ||
    !powderProductId
  ) {
    return { ok: false as const, error: "Please review the form.", intent };
  }

  let inventory: WorkshopInventory;
  try {
    inventory = await getWorkshopInventory();
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "Inventory could not be refreshed.",
      intent,
    };
  }
  const primerProduct = inventory.products.find(
    (product) => product.id === primerProductId && product.type === "Primer" && !product.archived,
  );
  const powderProduct = inventory.products.find(
    (product) => product.id === powderProductId && product.type === "Powder" && !product.archived,
  );
  if (!primerProduct || !powderProduct) {
    return {
      ok: false as const,
      error: "Select an active primer and powder product.",
      intent,
    };
  }
  const productLabel = (brand: string | null | undefined, productName: string) =>
    [brand, productName].filter(Boolean).join(" ");
  const recipe = {
    name,
    caliber,
    oal: text(form, "oal"),
    bulletWeight,
    bulletForm: text(form, "bulletForm"),
    primer: productLabel(primerProduct.brand, primerProduct.name),
    primerProductId,
    powder: productLabel(powderProduct.brand, powderProduct.name),
    powderProductId,
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
