import { useEffect, useRef, useState } from "react";
import type {
  WorkshopInventory,
  WorkshopProductionLotAllocation,
  WorkshopProductionRecord,
  WorkshopRecipe,
} from "@morwalpizvideo/models";
import type { FetcherWithComponents } from "react-router";
import { formatDate, formatNumber, localDateInputValue } from "../i18n-formatters";
import { useI18n } from "../i18n";
import { allocateOldestLots, getAllocatableLots, isValidOldestLotAllocation } from "./productionAllocations";
import type { ProductionActionResult } from "./submitProduction";

type ProductionFetcher = FetcherWithComponents<ProductionActionResult>;

function newIdempotencyKey(): string {
  return crypto.randomUUID();
}

function allocationsFor(
  inventory: WorkshopInventory,
  productId: string,
  quantity: number,
  previous: WorkshopProductionLotAllocation[],
): WorkshopProductionLotAllocation[] {
  return allocateOldestLots(inventory, productId, quantity, previous);
}

function LotAllocationSummary({
  inventory,
  productId,
  quantity,
  previous,
  allocations,
  label,
}: {
  inventory: WorkshopInventory;
  productId: string;
  quantity: number;
  previous: WorkshopProductionLotAllocation[];
  allocations: WorkshopProductionLotAllocation[];
  label: string;
}) {
  const { culture, t } = useI18n();
  const lots = getAllocatableLots(inventory, productId, previous);
  const allocationById = new Map(allocations.map((allocation) => [allocation.lotId, allocation.quantity]));
  const allocatedQuantity = allocations.reduce((sum, allocation) => sum + allocation.quantity, 0);

  return (
    <section className="production-lot-selection" aria-label={t(label)}>
      <h3>{t(label)}</h3>
      <p>{t("Oldest available lots are selected first.")}</p>
      {lots.length ? (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{t("Purchase date")}</th>
                <th>{t("Lot")}</th>
                <th>{t("Available")}</th>
                <th>{t("Selected")}</th>
              </tr>
            </thead>
            <tbody>
              {lots.map(({ lot, availableQuantity }) => (
                <tr key={lot.id}>
                  <td>{formatDate(lot.purchaseDate, culture)}</td>
                  <td>{lot.id}</td>
                  <td>{formatNumber(availableQuantity, culture)}</td>
                  <td>{formatNumber(allocationById.get(lot.id) ?? 0, culture)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p>{t("No available lots for this product.")}</p>
      )}
      <p className={allocatedQuantity === quantity ? "form-success" : "form-error"} role="status">
        {allocatedQuantity === quantity
          ? t("{count} units allocated.", { count: formatNumber(allocatedQuantity, culture) })
          : t("Insufficient stock: {allocated} of {required} units can be allocated.", {
              allocated: formatNumber(allocatedQuantity, culture),
              required: formatNumber(quantity, culture),
            })}
      </p>
    </section>
  );
}

export default function ProductionForm({
  recipes,
  inventory,
  fetcher,
  fixedRecipe,
  record,
  minimumQuantity = 1,
  onSaved,
  onCancel,
}: {
  recipes: WorkshopRecipe[];
  inventory: WorkshopInventory;
  fetcher: ProductionFetcher;
  fixedRecipe?: WorkshopRecipe;
  record?: WorkshopProductionRecord;
  minimumQuantity?: number;
  onSaved?: () => void;
  onCancel?: () => void;
}) {
  const { culture, t } = useI18n();
  const editing = Boolean(record);
  const session = useRef(record?.session).current;
  const [minimumQuantityForRecord] = useState(minimumQuantity);
  const [idempotencyKey, setIdempotencyKey] = useState(newIdempotencyKey);
  const [recipeId, setRecipeId] = useState(session?.recipeId ?? fixedRecipe?.id ?? "");
  const [quantityText, setQuantityText] = useState(String(session?.quantityProduced ?? ""));
  const quantity = Number(quantityText);
  const [productionDate, setProductionDate] = useState(
    session?.productionDate.slice(0, 10) ?? localDateInputValue(),
  );
  const [notes, setNotes] = useState(session?.notes ?? "");
  const [bulletProductId, setBulletProductId] = useState(session?.bulletProductId ?? "");
  const [primerLotAllocations, setPrimerLotAllocations] = useState(
    session?.primerLotAllocations ?? [],
  );
  const [bulletLotAllocations, setBulletLotAllocations] = useState(
    session?.bulletLotAllocations ?? [],
  );
  const [visibleResult, setVisibleResult] = useState<ProductionActionResult>();
  const lastResult = useRef<ProductionActionResult | undefined>(fetcher.data);

  const selectedRecipe = recipes.find((recipe) => recipe.id === recipeId) ?? fixedRecipe;
  const primerProductId = session?.recipeSnapshot.primerProductId ?? selectedRecipe?.primerProductId ?? "";
  const powderProductId = session?.recipeSnapshot.powderProductId ?? selectedRecipe?.powderProductId ?? "";
  const recipeHasSupplyReferences = Boolean(
    session?.recipeSnapshot.primerProductId && session.recipeSnapshot.powderProductId,
  );
  const recipeVersion = recipeHasSupplyReferences
    ? session?.recipeSnapshot.version ?? 0
    : selectedRecipe?.version ?? session?.recipeSnapshot.version ?? 0;
  const primerProduct = inventory.products.find(
    (product) => product.id === primerProductId && product.type === "Primer",
  );
  const powderProduct = inventory.products.find(
    (product) => product.id === powderProductId && product.type === "Powder",
  );
  const primerPrevious = session?.primerLotAllocations ?? [];
  const bulletPrevious = session?.bulletLotAllocations ?? [];
  const bulletProducts = inventory.products.filter(
    (product) => product.type === "Bullet" && !product.archived,
  );
  const priorBulletProduct = session
    ? inventory.products.find((product) => product.id === session.bulletProductId && product.type === "Bullet")
    : undefined;
  const availableBulletProducts = priorBulletProduct && priorBulletProduct.archived
    ? [...bulletProducts, priorBulletProduct]
    : bulletProducts;
  const requiredQuantity = Number.isSafeInteger(quantity) && quantity > 0 ? quantity : 0;
  const validAllocations = requiredQuantity > 0
    && isValidOldestLotAllocation(
      inventory,
      primerProductId,
      requiredQuantity,
      primerLotAllocations,
      session,
      "primer",
    )
    && isValidOldestLotAllocation(
      inventory,
      bulletProductId,
      requiredQuantity,
      bulletLotAllocations,
      session,
      "bullet",
    );
  const referencesReady = Boolean(
    primerProduct
    && powderProduct
    && (editing || (!primerProduct.archived && !powderProduct.archived)),
  );
  const canSubmit = referencesReady
    && Boolean(bulletProductId)
    && validAllocations
    && requiredQuantity >= minimumQuantityForRecord
    && fetcher.state === "idle";

  useEffect(() => {
    if (fetcher.state !== "idle") {
      setVisibleResult(undefined);
      return;
    }
    if (!fetcher.data || fetcher.data === lastResult.current) return;
    lastResult.current = fetcher.data;
    setVisibleResult(fetcher.data);
    if (fetcher.data.ok) {
      setIdempotencyKey(newIdempotencyKey());
      if (editing) {
        onSaved?.();
      } else {
        setRecipeId(fixedRecipe?.id ?? "");
        setQuantityText("");
        setProductionDate(localDateInputValue());
        setNotes("");
        setBulletProductId("");
        setPrimerLotAllocations([]);
        setBulletLotAllocations([]);
      }
    } else if (!fetcher.data.uncertain) {
      setIdempotencyKey(newIdempotencyKey());
    }
  }, [editing, fetcher.data, fetcher.state, fixedRecipe?.id, onSaved]);

  const changeRecipe = (nextRecipeId: string) => {
    const nextRecipe = recipes.find((recipe) => recipe.id === nextRecipeId);
    setRecipeId(nextRecipeId);
    setPrimerLotAllocations(allocationsFor(
      inventory,
      nextRecipe?.primerProductId ?? "",
      requiredQuantity,
      [],
    ));
    setBulletLotAllocations(allocationsFor(inventory, bulletProductId, requiredQuantity, []));
  };

  const changeQuantity = (value: string) => {
    const nextQuantity = Number(value);
    setQuantityText(value);
    const validNextQuantity = Number.isSafeInteger(nextQuantity) && nextQuantity > 0 ? nextQuantity : 0;
    setPrimerLotAllocations(allocationsFor(
      inventory,
      primerProductId,
      validNextQuantity,
      primerPrevious,
    ));
    setBulletLotAllocations(allocationsFor(
      inventory,
      bulletProductId,
      validNextQuantity,
      bulletPrevious,
    ));
  };

  const changeBulletProduct = (nextProductId: string) => {
    setBulletProductId(nextProductId);
    setBulletLotAllocations(allocationsFor(
      inventory,
      nextProductId,
      requiredQuantity,
      bulletPrevious,
    ));
  };

  const result = visibleResult;
  const resultMessage = result && !result.ok && result.uncertain
    ? t("The save outcome is uncertain. Check production records and stock before trying again.")
    : result && !result.ok
      ? result.error
      : null;

  return (
    <fetcher.Form method="post" className="panel form-panel ammunition-production-form">
      <input type="hidden" name="intent" value={editing ? "update-production" : "produce"} />
      <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
      {session && (
        <>
          <input type="hidden" name="sessionId" value={session.id} />
          <input type="hidden" name="expectedRevision" value={session.revision} />
        </>
      )}
      <input type="hidden" name="recipeId" value={recipeId} />
      <input type="hidden" name="recipeVersion" value={recipeVersion} />
      <input type="hidden" name="primerProductId" value={primerProductId} />
      <input type="hidden" name="powderProductId" value={powderProductId} />
      <input type="hidden" name="primerLotAllocations" value={JSON.stringify(primerLotAllocations)} />
      <input type="hidden" name="bulletLotAllocations" value={JSON.stringify(bulletLotAllocations)} />
      <h2>{t(editing ? "Edit production record" : "Record production")}</h2>
      {!editing && !fixedRecipe && (
        <label>
          {t("Recipe")}
          <select
            name="recipeSelect"
            required
            value={recipeId}
            onChange={(event) => changeRecipe(event.target.value)}
          >
            <option value="">{t("Select recipe")}</option>
            {recipes.filter((recipe) => recipe.status === "Active").map((recipe) => (
              <option value={recipe.id} key={recipe.id}>{recipe.name} · {recipe.caliber}</option>
            ))}
          </select>
        </label>
      )}
      {(editing || fixedRecipe) && (
        <p className="production-recipe-reference">
          <strong>{editing ? session?.recipeSnapshot.name : fixedRecipe?.name}</strong>
          {" · "}
          {editing ? session?.recipeSnapshot.caliber : fixedRecipe?.caliber}
          {editing && <span> · {t("Recipe identity stays fixed for shooting history.")}</span>}
        </p>
      )}
      {!referencesReady && recipeId && (
        <p className="status-note" role="status">
          {t("This recipe needs primer and powder products before production.")}
          {" "}
          <a href="/ammunition?tab=recipes">{t("Update recipe")}</a>
        </p>
      )}
      <div className="form-grid">
        <label>
          {t("Quantity produced")}
          <input
            name="quantityProduced"
            type="number"
            min={String(minimumQuantity)}
            step="1"
            required
            value={quantityText}
            onChange={(event) => changeQuantity(event.target.value)}
          />
        </label>
        <label>
          {t("Production date")}
          <input
            name="productionDate"
            type="date"
            required
            value={productionDate}
            onChange={(event) => setProductionDate(event.target.value)}
          />
        </label>
        <label>
          {t("Bullet product")}
          <select
            name="bulletProductId"
            required
            value={bulletProductId}
            onChange={(event) => changeBulletProduct(event.target.value)}
          >
            <option value="">{t("Select bullet product")}</option>
            {availableBulletProducts.map((product) => (
              <option value={product.id} key={product.id}>{product.name}{product.brand ? ` · ${product.brand}` : ""}</option>
            ))}
          </select>
        </label>
        <label>
          {t("Notes")}
          <textarea name="notes" value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>
      </div>
      {referencesReady && (
        <div className="production-allocation-grid">
          <LotAllocationSummary
            inventory={inventory}
            productId={primerProductId}
            quantity={requiredQuantity}
            previous={primerPrevious}
            allocations={primerLotAllocations}
            label="Primer lot allocations"
          />
          <LotAllocationSummary
            inventory={inventory}
            productId={bulletProductId}
            quantity={requiredQuantity}
            previous={bulletPrevious}
            allocations={bulletLotAllocations}
            label="Bullet lot allocations"
          />
        </div>
      )}
      {editing && minimumQuantityForRecord > 1 && (
        <p className="status-note">
          {t("At least {count} units have already been consumed and must remain in this record.", {
            count: formatNumber(minimumQuantityForRecord, culture),
          })}
        </p>
      )}
      {resultMessage && (
        <p className={result?.ok ? "form-success" : "form-error"} role="alert">
          {resultMessage}
        </p>
      )}
      {result?.ok && fetcher.state === "idle" && (
        <p className="form-success" role="status">{t("Production recorded.")}</p>
      )}
      <div className="feedback-actions">
        {editing && onCancel && (
          <button type="button" className="button button-quiet" onClick={onCancel}>
            {t("Cancel")}
          </button>
        )}
        <button className="button button-primary" disabled={!canSubmit}>
          {fetcher.state !== "idle"
            ? t("Saving…")
            : t(editing ? "Save production changes" : "Create batch")}
        </button>
      </div>
    </fetcher.Form>
  );
}

export function getProductionMinimumQuantity(
  sessionId: string,
  batches: { productionSessionId: string; producedQuantity: number; onHandQuantity: number }[],
): number {
  const batch = batches.find((item) => item.productionSessionId === sessionId);
  return batch ? Math.max(1, batch.producedQuantity - batch.onHandQuantity) : 1;
}
