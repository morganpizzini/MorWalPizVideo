import {
  ApiResponseError,
  getWorkshopBatches,
  getWorkshopInventory,
  getWorkshopProductionRecords,
  recordWorkshopProduction,
  updateWorkshopProduction,
} from "@morwalpizvideo/services";
import type {
  WorkshopProductionLotAllocation,
  WorkshopProductionSession,
} from "@morwalpizvideo/models";
import { isValidOldestLotAllocation } from "./productionAllocations";

export type ProductionIntent = "produce" | "update-production";

export type ProductionActionResult =
  | { ok: true; intent: ProductionIntent }
  | {
      ok: false;
      error: string;
      uncertain: boolean;
      intent: ProductionIntent;
      idempotencyKey: string;
    };

const text = (form: FormData, key: string): string => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

function isWorkshopAllocation(value: unknown): value is WorkshopProductionLotAllocation {
  return typeof value === "object"
    && value !== null
    && "lotId" in value
    && typeof value.lotId === "string"
    && "quantity" in value
    && typeof value.quantity === "number"
    && Number.isSafeInteger(value.quantity)
    && value.quantity > 0;
}

function allocations(form: FormData, field: string): WorkshopProductionLotAllocation[] | null {
  try {
    const parsed: unknown = JSON.parse(text(form, field));
    return Array.isArray(parsed) && parsed.every(isWorkshopAllocation) ? parsed : null;
  } catch {
    return null;
  }
}

function failed(
  intent: ProductionIntent,
  idempotencyKey: string,
  error: string,
  uncertain = false,
): ProductionActionResult {
  return { ok: false, error, uncertain, intent, idempotencyKey };
}

export async function submitProduction(form: FormData): Promise<ProductionActionResult> {
  const formIntent = text(form, "intent");
  if (formIntent && formIntent !== "produce" && formIntent !== "update-production") {
    return failed("produce", text(form, "idempotencyKey"), "Unsupported production action.");
  }
  const intent = formIntent === "update-production" ? "update-production" : "produce";
  const idempotencyKey = text(form, "idempotencyKey");
  const recipeId = text(form, "recipeId");
  const recipeVersion = Number(text(form, "recipeVersion"));
  const productionDate = text(form, "productionDate");
  const quantityProduced = Number(text(form, "quantityProduced"));
  const bulletProductId = text(form, "bulletProductId");
  const primerLotAllocations = allocations(form, "primerLotAllocations");
  const bulletLotAllocations = allocations(form, "bulletLotAllocations");
  const notes = text(form, "notes");
  const sessionId = text(form, "sessionId");
  const expectedRevision = Number(text(form, "expectedRevision"));

  if (
    !idempotencyKey
    || !recipeId
    || !Number.isSafeInteger(recipeVersion)
    || recipeVersion < 1
    || !productionDate
    || !Number.isSafeInteger(quantityProduced)
    || quantityProduced < 1
    || !bulletProductId
    || !primerLotAllocations
    || !bulletLotAllocations
  ) {
    return failed(intent, idempotencyKey, "Please review the form.");
  }

  if (typeof navigator !== "undefined" && !navigator.onLine) {
    return failed(intent, idempotencyKey, "Inventory production is only available while online.");
  }

  let inventory;
  try {
    inventory = await getWorkshopInventory();
  } catch (error) {
    return failed(
      intent,
      idempotencyKey,
      error instanceof Error ? error.message : "Inventory could not be refreshed.",
    );
  }

  let session: WorkshopProductionSession | undefined;
  if (intent === "update-production") {
    if (!sessionId || !Number.isSafeInteger(expectedRevision) || expectedRevision < 0) {
      return failed(intent, idempotencyKey, "Please review the form.");
    }

    try {
      const [records, batches] = await Promise.all([
        getWorkshopProductionRecords(),
        getWorkshopBatches(),
      ]);
      session = records.find((record) => record.session.id === sessionId)?.session;
      const batch = session
        ? batches.find((item) => item.id === session?.batchId)
        : undefined;
      if (!batch) {
        return failed(intent, idempotencyKey, "The production batch could not be loaded.");
      }
      const consumedQuantity = Math.max(0, batch.producedQuantity - batch.onHandQuantity);
      if (quantityProduced < consumedQuantity) {
        return failed(
          intent,
          idempotencyKey,
          `Production quantity cannot be lower than ${consumedQuantity} units already consumed from this batch.`,
        );
      }
    } catch (error) {
      return failed(
        intent,
        idempotencyKey,
        error instanceof Error ? error.message : "Production records could not be refreshed.",
      );
    }

    if (!session || session.revision !== expectedRevision || session.recipeId !== recipeId) {
      return failed(
        intent,
        idempotencyKey,
        "This production record changed or is no longer available. Reload before editing it.",
      );
    }
  }

  const recipe = session?.recipeSnapshot;
  const primerProductId = recipe?.primerProductId
    ?? text(form, "primerProductId");
  const powderProductId = recipe?.powderProductId ?? text(form, "powderProductId");
  if (!primerProductId || !powderProductId) {
    return failed(
      intent,
      idempotencyKey,
      "Update this recipe with primer and powder products before recording production.",
    );
  }
  const primerProduct = inventory.products.find(
    (product) => product.id === primerProductId && product.type === "Primer",
  );
  const powderProduct = inventory.products.find(
    (product) => product.id === powderProductId && product.type === "Powder",
  );
  const bulletProduct = inventory.products.find(
    (product) => product.id === bulletProductId && product.type === "Bullet",
  );
  if (
    !primerProduct
    || !powderProduct
    || !bulletProduct
    || (intent === "produce" && (primerProduct.archived || powderProduct.archived || bulletProduct.archived))
    || (intent === "update-production" && bulletProduct.archived && bulletProductId !== session?.bulletProductId)
  ) {
    return failed(intent, idempotencyKey, "Select active products for this production.");
  }

  if (
    !isValidOldestLotAllocation(
      inventory,
      primerProductId,
      quantityProduced,
      primerLotAllocations,
      session,
      "primer",
    )
    || !isValidOldestLotAllocation(
      inventory,
      bulletProductId,
      quantityProduced,
      bulletLotAllocations,
      session,
      "bullet",
    )
  ) {
    return failed(
      intent,
      idempotencyKey,
      "The selected lots no longer match oldest-first available stock. Reload inventory and select again.",
    );
  }

  const body = {
    recipeId,
    recipeVersion,
    productionDate,
    quantityProduced,
    bulletProductId,
    primerLotAllocations,
    bulletLotAllocations,
    notes: notes || null,
    idempotencyKey,
  };

  try {
    if (intent === "update-production") {
      await updateWorkshopProduction(sessionId, { ...body, expectedRevision });
    } else {
      await recordWorkshopProduction(body);
    }
    return { ok: true, intent };
  } catch (error) {
    const uncertain = !(error instanceof ApiResponseError)
      || (error.status ?? 500) >= 500
      || error.status === 409;
    return failed(
      intent,
      idempotencyKey,
      error instanceof Error ? error.message : "The request could not be completed.",
      uncertain,
    );
  }
}
