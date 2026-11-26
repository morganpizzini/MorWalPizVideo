import { EmptyState, PageTitle } from "@morwalpiz/layout";
import type {
  WorkshopComponentDefinition,
  WorkshopInventory,
  WorkshopPhysicalComponent,
} from "@morwalpizvideo/models";
import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useLoaderData, useFetcher } from "react-router";
import { useI18n } from "../../i18n";
import { formatDate, formatNumber, localDateInputValue } from "../../i18n-formatters";
import { DeferredRoute } from "../shared";
import type { InventoryActionResult } from "./action";
import type { InventoryLoaderData, InventoryRouteData } from "./loader";

type MutationState = "idle" | "busy" | "uncertain";

function currencyAmount(amount: number, currency: string, culture: string) {
  return `${new Intl.NumberFormat(culture, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)} ${currency}`;
}

function LoadingError() {
  const { t } = useI18n();
  return (
    <div className="state state-error" role="alert">
      <strong>{t("Inventory could not be loaded.")}</strong>
      <p>{t("Reconnect and reload this page to inspect the latest inventory.")}</p>
    </div>
  );
}

function InventoryMutationForm({
  intent,
  mutationId,
  label,
  blocked,
  reportState,
  children,
}: {
  intent: string;
  mutationId: string;
  label: string;
  blocked: boolean;
  reportState: (id: string, state: MutationState) => void;
  children: ReactNode;
}) {
  const { t } = useI18n();
  const fetcher = useFetcher<InventoryActionResult>();
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [uncertain, setUncertain] = useState(false);
  const [visibleResult, setVisibleResult] = useState<InventoryActionResult>();
  const lastResult = useRef(fetcher.data);

  useEffect(() => {
    if (fetcher.state !== "idle") {
      reportState(mutationId, "busy");
      setVisibleResult(undefined);
      return;
    }
    if (fetcher.data && fetcher.data !== lastResult.current) {
      lastResult.current = fetcher.data;
      setVisibleResult(fetcher.data);
      if (fetcher.data.ok) {
        setIdempotencyKey(crypto.randomUUID());
        setUncertain(false);
        reportState(mutationId, "idle");
      } else if (fetcher.data.uncertain) {
        setUncertain(true);
        reportState(mutationId, "uncertain");
      } else {
        setIdempotencyKey(crypto.randomUUID());
        reportState(mutationId, "idle");
      }
      return;
    }
    if (!uncertain) reportState(mutationId, "idle");
  }, [fetcher.data, fetcher.state, mutationId, reportState, uncertain]);

  return (
    <fetcher.Form method="post" className="inventory-mutation-form">
      <input type="hidden" name="intent" value={intent} />
      <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
      {children}
      {visibleResult?.ok && fetcher.state === "idle" && (
        <p className="form-success" role="status">{t("Inventory update recorded.")}</p>
      )}
      {visibleResult && !visibleResult.ok && fetcher.state === "idle" && (
        <div className={visibleResult.uncertain ? "inventory-uncertain" : "form-error"} role="alert">
          <strong>
            {visibleResult.uncertain
              ? t("The save outcome is uncertain. Inspect inventory before continuing.")
              : visibleResult.error}
          </strong>
          {visibleResult.uncertain && (
            <>
              <p>{visibleResult.error}</p>
              <button type="button" className="button button-quiet" onClick={() => window.location.reload()}>
                {t("Reload inventory")}
              </button>
            </>
          )}
        </div>
      )}
      <button
        className="button button-primary"
        disabled={blocked || fetcher.state !== "idle" || uncertain}
      >
        {fetcher.state !== "idle" ? t("Saving…") : t(label)}
      </button>
    </fetcher.Form>
  );
}

function Totals({ title, totals }: { title: string; totals: WorkshopInventory["spending"] }) {
  const { culture, t } = useI18n();
  return (
    <section className="inventory-finance-column">
      <h3>{t(title)}</h3>
      {totals.length ? (
        <ul className="inventory-currency-list">
          {totals.map((entry) => (
            <li key={`${title}-${entry.currency}`}>
              <span>{entry.currency}</span>
              <strong>{currencyAmount(entry.total, entry.currency, culture)}</strong>
            </li>
          ))}
        </ul>
      ) : <p>{t("No recorded amounts.")}</p>}
    </section>
  );
}

function InventoryContent({
  inventory,
  components,
  definitions,
}: {
  inventory: WorkshopInventory;
  components: WorkshopPhysicalComponent[];
  definitions: WorkshopComponentDefinition[];
}) {
  const { culture, t } = useI18n();
  const [mutationStates, setMutationStates] = useState<Record<string, MutationState>>({});
  const reportState = useCallback((id: string, state: MutationState) => {
    setMutationStates((current) => current[id] === state
      ? current
      : { ...current, [id]: state });
  }, []);
  const mutationBlocked = Object.values(mutationStates).some((state) => state !== "idle");
  const productById = new Map(inventory.products.map((product) => [product.id, product]));
  const componentById = new Map(components.map((component) => [component.id, component]));
  const definitionById = new Map(definitions.map((definition) => [definition.id, definition]));
  const componentByPurchaseId = new Map(inventory.componentPurchases.map((purchase) => [purchase.componentId, purchase]));
  const missingComponentIds = new Set(inventory.missingComponentPrices);
  const pricedComponentIds = new Set(inventory.componentPurchases.map((purchase) => purchase.componentId));
  const orphanedPurchases = inventory.componentPurchases.filter((purchase) => !componentById.has(purchase.componentId));

  const componentLabel = (component: WorkshopPhysicalComponent) => {
    const definition = definitionById.get(component.definitionId);
    return [definition?.name, component.manufacturer, component.model, component.identificationNote]
      .filter(Boolean)
      .join(" · ") || component.id;
  };

  return (
    <>
      <section className="panel inventory-policy-note">
        <h2>{t("Online inventory")}</h2>
        <p>{t("Inventory and finance changes are never saved offline or queued for sync.")}</p>
        <p>{t("An interrupted request can leave a partial update. If the result is uncertain, inspect the saved records before continuing.")}</p>
      </section>

      <section className="panel">
        <h2>{t("Recorded finance")}</h2>
        <div className="inventory-finance-grid">
          <Totals title="Purchase spending" totals={inventory.spending} />
          <Totals title="Partial consumption costs" totals={inventory.partialConsumptionCosts} />
        </div>
        <p className="status-note">{t("These totals are grouped by currency and are not a complete unit-cost calculation.")}</p>
      </section>

      <section className="panel">
        <div className="section-heading">
          <h2>{t("Supply products")}</h2>
          <span>{t("{count} products", { count: formatNumber(inventory.products.length, culture) })}</span>
        </div>
        <div className="inventory-forms-grid">
          <div>
            <h3>{t("Add a product")}</h3>
            <InventoryMutationForm
              intent="create-product"
              mutationId="create-product"
              label="Save product"
              blocked={mutationBlocked}
              reportState={reportState}
            >
              <label>
                {t("Type")}
                <select name="type" required defaultValue="Primer">
                  <option value="Primer">{t("Primer")}</option>
                  <option value="Bullet">{t("Bullet")}</option>
                  <option value="Powder">{t("Powder")}</option>
                  <option value="Case">{t("Case")}</option>
                  <option value="CommercialAmmunition">{t("Commercial ammunition")}</option>
                </select>
              </label>
              <label>{t("Brand")}<input name="brand" maxLength={120} /></label>
              <label>{t("Product name or number")}<input name="name" required maxLength={160} /></label>
            </InventoryMutationForm>
          </div>
          <div>
            <h3>{t("Record a purchase")}</h3>
            {inventory.products.some((product) => !product.archived) ? (
              <InventoryMutationForm
                intent="record-purchase"
                mutationId="record-purchase"
                label="Save purchase"
                blocked={mutationBlocked}
                reportState={reportState}
              >
                <label>
                  {t("Product")}
                  <select name="productId" required defaultValue="">
                    <option value="">{t("Select product")}</option>
                    {inventory.products.filter((product) => !product.archived).map((product) => (
                      <option key={product.id} value={product.id}>
                        {t(product.type)} · {[product.brand, product.name].filter(Boolean).join(" ")}
                      </option>
                    ))}
                  </select>
                </label>
                <div className="form-grid">
                  <label>{t("Purchase date")}<input type="date" name="purchaseDate" required defaultValue={localDateInputValue()} /></label>
                  <label>{t("Quantity purchased")}<input type="number" name="quantity" min="1" step="1" required /></label>
                  <label>{t("Total price")}<input type="number" name="totalPrice" min="0" step="0.01" required /></label>
                  <label>{t("Currency (ISO code)")}<input name="currency" maxLength={3} minLength={3} pattern="[A-Za-z]{3}" required /></label>
                </div>
              </InventoryMutationForm>
            ) : <p>{t("Create a product before recording a purchase.")}</p>}
          </div>
        </div>
        {inventory.products.length ? (
          <div className="table-wrap mt-3">
            <table>
              <thead><tr><th>{t("Type")}</th><th>{t("Brand")}</th><th>{t("Product")}</th><th>{t("Status")}</th></tr></thead>
              <tbody>{inventory.products.map((product) => (
                <tr key={product.id}>
                  <td>{t(product.type)}</td><td>{product.brand || "—"}</td><td>{product.name}</td>
                  <td>{t(product.archived ? "Archived" : "Active")}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        ) : <EmptyState title={t("No supply products")} />}
      </section>

      <section className="panel">
        <h2>{t("Purchase lots and stock")}</h2>
        {inventory.lots.length ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{t("Product")}</th><th>{t("Purchase date")}</th><th>{t("Purchased")}</th>
                  <th>{t("Available")}</th><th>{t("Purchase cost")}</th><th>{t("Stock controls")}</th>
                </tr>
              </thead>
              <tbody>{inventory.lots.map((lot) => {
                const product = productById.get(lot.productId);
                const canCount = product?.type === "Primer" || product?.type === "Bullet" || product?.type === "Case";
                const packageStatus = product?.type === "Powder"
                  || product?.type === "Case"
                  || product?.type === "CommercialAmmunition";
                return (
                  <tr key={lot.id}>
                    <td>{product ? `${t(product.type)} · ${product.name}` : lot.productId}</td>
                    <td>{formatDate(lot.purchaseDate, culture)}</td>
                    <td>{formatNumber(lot.purchasedQuantity, culture)}</td>
                    <td>{formatNumber(product?.type === "Case" && lot.itemCount != null ? lot.itemCount : lot.availableQuantity, culture)}</td>
                    <td>{currencyAmount(lot.totalPrice, lot.currency, culture)}</td>
                    <td className="inventory-lot-controls">
                      {canCount && (
                        <InventoryMutationForm
                          intent="adjust-stock"
                          mutationId={`adjust:${lot.id}`}
                          label="Correct physical count"
                          blocked={mutationBlocked}
                          reportState={reportState}
                        >
                          <input type="hidden" name="lotId" value={lot.id} />
                          <input type="hidden" name="expectedRevision" value={lot.revision} />
                          <label>
                            {t("Physical count")}
                            <input name="physicalCount" type="number" min="0" step="1" required defaultValue={product?.type === "Case" && lot.itemCount != null ? lot.itemCount : lot.availableQuantity} />
                          </label>
                          <label>{t("Reason")}<input name="reason" required maxLength={500} /></label>
                        </InventoryMutationForm>
                      )}
                      {packageStatus && (
                        <InventoryMutationForm
                          intent="set-lot-status"
                          mutationId={`status:${lot.id}`}
                          label={lot.isEmpty ? "Reopen package" : "Mark package empty"}
                          blocked={mutationBlocked}
                          reportState={reportState}
                        >
                          <input type="hidden" name="lotId" value={lot.id} />
                          <input type="hidden" name="expectedRevision" value={lot.revision} />
                          <input type="hidden" name="isEmpty" value={String(!lot.isEmpty)} />
                        </InventoryMutationForm>
                      )}
                    </td>
                  </tr>
                );
              })}</tbody>
            </table>
          </div>
        ) : <EmptyState title={t("No purchases recorded")} message={t("Record a purchase to create the first stock lot.")} />}
      </section>

      <section className="panel">
        <div className="section-heading">
          <h2>{t("Physical component prices")}</h2>
          {inventory.missingComponentPrices.length > 0 && (
            <span className="status-note">{t("{count} missing prices", { count: inventory.missingComponentPrices.length })}</span>
          )}
        </div>
        <p>{t("Enter a component's purchase price once. Later corrections update that record and retain its history.")}</p>
        {components.length ? (
          <div className="inventory-component-list">
            {components.map((component) => {
              const purchase = componentByPurchaseId.get(component.id);
              const intent = purchase ? "correct-component-purchase" : "component-purchase";
              return (
                <section key={component.id} className="inventory-component-row">
                  <div>
                    <h3>{componentLabel(component)}</h3>
                    <p>{purchase
                      ? t("Recorded price: {amount} on {date}", {
                          amount: currencyAmount(purchase.totalPrice, purchase.currency, culture),
                          date: formatDate(purchase.purchaseDate, culture),
                        })
                      : missingComponentIds.has(component.id)
                        ? t("Historical purchase price is unknown; no amount is included.")
                        : t("No purchase price recorded.")}</p>
                    {purchase && purchase.history.length > 1 && (
                      <details>
                        <summary>{t("Price correction history")} ({purchase.history.length})</summary>
                        <ul>
                          {purchase.history.map((revision) => (
                            <li key={revision.revision}>
                              v{revision.revision}: {currencyAmount(revision.totalPrice, revision.currency, culture)}
                              {" · "}{formatDate(revision.purchaseDate, culture)}
                              {revision.reason ? ` · ${revision.reason}` : ""}
                            </li>
                          ))}
                        </ul>
                      </details>
                    )}
                  </div>
                  <InventoryMutationForm
                    intent={intent}
                    mutationId={`component:${component.id}`}
                    label={purchase ? "Correct component price" : "Record component price"}
                    blocked={mutationBlocked}
                    reportState={reportState}
                  >
                    <input type="hidden" name="componentId" value={component.id} />
                    {purchase && <input type="hidden" name="expectedRevision" value={purchase.revision} />}
                    <div className="form-grid">
                      <label>{t("Purchase date")}<input name="purchaseDate" type="date" required defaultValue={purchase?.purchaseDate.slice(0, 10) ?? localDateInputValue()} /></label>
                      <label>{t("Total price")}<input name="totalPrice" type="number" min="0" step="0.01" required defaultValue={purchase?.totalPrice ?? ""} /></label>
                      <label>{t("Currency (ISO code)")}<input name="currency" maxLength={3} minLength={3} pattern="[A-Za-z]{3}" required defaultValue={purchase?.currency ?? ""} /></label>
                      {purchase && <label>{t("Reason")}<input name="reason" required maxLength={500} /></label>}
                    </div>
                  </InventoryMutationForm>
                </section>
              );
            })}
          </div>
        ) : <EmptyState title={t("No physical components")} message={<Link to="/firearms?tab=inventory">{t("Create a physical component")}</Link>} />}
        {orphanedPurchases.length > 0 && (
          <section className="inventory-orphaned-prices">
            <h3>{t("Purchase history for unavailable components")}</h3>
            <ul>{orphanedPurchases.map((purchase) => (
              <li key={purchase.id}>
                {purchase.componentId}: {currencyAmount(purchase.totalPrice, purchase.currency, culture)}
                {" · "}{formatDate(purchase.purchaseDate, culture)}
              </li>
            ))}</ul>
          </section>
        )}
        {inventory.missingComponentPrices.some((id) => !componentById.has(id) && !pricedComponentIds.has(id)) && (
          <p className="status-note">{t("Some missing-price component records are no longer available for editing.")}</p>
        )}
      </section>
    </>
  );
}

export default function Inventory() {
  const { t } = useI18n();
  const data = useLoaderData() as InventoryLoaderData;
  return (
    <div>
      <PageTitle title={t("Inventory and finance")} subtitle={t("Online purchase records, stock lots and component costs.")} />
      {!data.online && (
        <section className="panel" role="status">
          <h2>{t("Inventory is online-only")}</h2>
          <p>{t("Reconnect to load or change inventory. No offline inventory operation was saved.")}</p>
        </section>
      )}
      <DeferredRoute
        data={data.data}
        fallback={<section className="panel" aria-busy="true">{t("Loading inventory…")}</section>}
        errorElement={<LoadingError />}
      >
        {(resolved: InventoryRouteData) => resolved
          ? <InventoryDataContent resolved={resolved} />
          : null}
      </DeferredRoute>
    </div>
  );
}

function InventoryDataContent({ resolved }: { resolved: Exclude<InventoryRouteData, null> }) {
  return <InventoryContent inventory={resolved[0]} components={resolved[1]} definitions={resolved[2]} />;
}
