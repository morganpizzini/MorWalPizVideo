import {
  ApiResponseError,
  adjustWorkshopInventoryLot,
  correctWorkshopComponentPurchase,
  createWorkshopInventoryProduct,
  deleteWorkshopInventoryProduct,
  recordWorkshopComponentPurchase,
  recordWorkshopInventoryPurchase,
  setWorkshopInventoryProductArchived,
  setWorkshopInventoryLotStatus,
  updateWorkshopInventoryProduct,
} from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

const supplyTypes = ["Primer", "Bullet", "Powder", "Case", "CommercialAmmunition"] as const;

export type InventoryActionResult =
  | { ok: true; intent: string; idempotencyKey: string }
  | {
      ok: false;
      intent: string;
      idempotencyKey: string;
      error: string;
      uncertain: boolean;
    };

const text = (form: FormData, key: string): string => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

function failure(intent: string, idempotencyKey: string, error: string, uncertain = false): InventoryActionResult {
  return { ok: false, intent, idempotencyKey, error, uncertain };
}

function integer(value: string, minimum = 0): number | undefined {
  if (!value) return undefined;
  const result = Number(value);
  return Number.isSafeInteger(result) && result >= minimum ? result : undefined;
}

function money(value: string): number | undefined {
  if (!/^\d+(?:\.\d{1,2})?$/.test(value)) return undefined;
  const result = Number(value);
  return Number.isFinite(result) && result >= 0 ? result : undefined;
}

function currency(value: string): boolean {
  return /^[A-Z]{3}$/.test(value);
}

function isUncertain(error: unknown): boolean {
  return !(error instanceof ApiResponseError)
    || (error.status ?? 500) >= 500
    || error.status === 409;
}

export async function action({ request }: ActionFunctionArgs): Promise<InventoryActionResult> {
  const form = await request.formData();
  const intent = text(form, "intent");
  const idempotencyKey = text(form, "idempotencyKey");
  if (!idempotencyKey) return failure(intent, idempotencyKey, "Please refresh the inventory form.");
  if (typeof navigator !== "undefined" && !navigator.onLine) {
    return failure(intent, idempotencyKey, "Inventory changes are only available while online.");
  }

  try {
    if (intent === "create-product") {
      const type = text(form, "type");
      const brand = text(form, "brand");
      const name = text(form, "name");
      const supplyType = supplyTypes.find((candidate) => candidate === type);
      if (!name || !supplyType) {
        return failure(intent, idempotencyKey, "Enter a product name and type.");
      }
      await createWorkshopInventoryProduct({
        type: supplyType,
        brand: brand || null,
        name,
        idempotencyKey,
      });
    } else if (intent === "update-product") {
      const productId = text(form, "productId");
      const name = text(form, "name");
      const brand = text(form, "brand");
      const expectedRevision = integer(text(form, "expectedRevision"));
      if (!productId || !name || expectedRevision === undefined) {
        return failure(intent, idempotencyKey, "Enter a product name before saving.");
      }
      await updateWorkshopInventoryProduct(productId, {
        name,
        brand: brand || null,
        expectedRevision,
        idempotencyKey,
      });
    } else if (intent === "archive-product") {
      const productId = text(form, "productId");
      const expectedRevision = integer(text(form, "expectedRevision"));
      const archived = text(form, "archived");
      if (!productId || expectedRevision === undefined || (archived !== "true" && archived !== "false")) {
        return failure(intent, idempotencyKey, "The product status form is incomplete.");
      }
      await setWorkshopInventoryProductArchived(productId, {
        archived: archived === "true",
        expectedRevision,
        idempotencyKey,
      });
    } else if (intent === "delete-product") {
      const productId = text(form, "productId");
      const expectedRevision = integer(text(form, "expectedRevision"));
      if (!productId || expectedRevision === undefined) {
        return failure(intent, idempotencyKey, "The product deletion form is incomplete.");
      }
      await deleteWorkshopInventoryProduct(productId, { expectedRevision, idempotencyKey });
    } else if (intent === "record-purchase") {
      const productId = text(form, "productId");
      const purchaseDate = text(form, "purchaseDate");
      const quantity = integer(text(form, "quantity"), 1);
      const packageCountValue = text(form, "packageCount");
      const unitsPerPackageValue = text(form, "unitsPerPackage");
      const packageCount = integer(packageCountValue, 1);
      const unitsPerPackage = integer(unitsPerPackageValue, 1);
      const totalPrice = money(text(form, "totalPrice"));
      const currencyCode = text(form, "currency").toUpperCase();
      const packageValuesValid = packageCount !== undefined && packageCount <= 10_000 &&
        unitsPerPackage !== undefined && unitsPerPackage <= 900_719_925_474;
      const hasPackageValues = Boolean(packageCountValue || unitsPerPackageValue);
      if (!productId || !purchaseDate || totalPrice === undefined || !currency(currencyCode) ||
          (hasPackageValues
            ? quantity !== undefined || !packageValuesValid
            : quantity === undefined || quantity > 10_000)) {
        return failure(intent, idempotencyKey, "Enter a valid product, date, quantity, price and three-letter currency.");
      }
      await recordWorkshopInventoryPurchase({
        productId,
        purchaseDate,
        ...(hasPackageValues
          ? { packageCount, unitsPerPackage }
          : { quantity }),
        totalPrice,
        currency: currencyCode,
        idempotencyKey,
      });
    } else if (intent === "adjust-stock") {
      const lotId = text(form, "lotId");
      const physicalCount = integer(text(form, "physicalCount"));
      const expectedRevision = integer(text(form, "expectedRevision"));
      const reason = text(form, "reason");
      if (!lotId || physicalCount === undefined || expectedRevision === undefined || !reason) {
        return failure(intent, idempotencyKey, "Enter a physical count and the reason for the correction.");
      }
      await adjustWorkshopInventoryLot({
        lotId,
        physicalCount,
        expectedRevision,
        reason,
        idempotencyKey,
      });
    } else if (intent === "set-lot-status") {
      const lotId = text(form, "lotId");
      const expectedRevision = integer(text(form, "expectedRevision"));
      const isEmpty = text(form, "isEmpty");
      if (!lotId || expectedRevision === undefined || (isEmpty !== "true" && isEmpty !== "false")) {
        return failure(intent, idempotencyKey, "The package status form is incomplete.");
      }
      await setWorkshopInventoryLotStatus(lotId, {
        isEmpty: isEmpty === "true",
        expectedRevision,
        idempotencyKey,
      });
    } else if (intent === "component-purchase" || intent === "correct-component-purchase") {
      const componentId = text(form, "componentId");
      const purchaseDate = text(form, "purchaseDate");
      const totalPrice = money(text(form, "totalPrice"));
      const currencyCode = text(form, "currency").toUpperCase();
      if (!componentId || !purchaseDate || totalPrice === undefined || !currency(currencyCode)) {
        return failure(intent, idempotencyKey, "Enter a valid date, price and three-letter currency.");
      }
      const common = { purchaseDate, totalPrice, currency: currencyCode, idempotencyKey };
      if (intent === "component-purchase") {
        await recordWorkshopComponentPurchase(componentId, common);
      } else {
        const expectedRevision = integer(text(form, "expectedRevision"), 1);
        const reason = text(form, "reason");
        if (expectedRevision === undefined) return failure(intent, idempotencyKey, "The purchase revision is missing.");
        await correctWorkshopComponentPurchase(componentId, {
          ...common,
          expectedRevision,
          ...(reason ? { reason } : {}),
        });
      }
    } else {
      return failure(intent, idempotencyKey, "Unsupported inventory action.");
    }

    return { ok: true, intent, idempotencyKey };
  } catch (error) {
    return failure(
      intent,
      idempotencyKey,
      error instanceof Error ? error.message : "The request could not be completed.",
      isUncertain(error),
    );
  }
}
