import { useEffect, useState } from "react";
import QRCode from "qrcode";
import { Link, useAsyncError, useFetcher, useLoaderData, useRouteError, isRouteErrorResponse } from "react-router";
import { PageTitle } from "@morwalpiz/layout";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import type { RecipeProduction, RecipeProductionLoaderResources } from "./loader";
import { DeferredRoute } from "../shared";
import { DetailSkeleton, PanelSkeleton } from "../../components/loading/LoadingPrimitives";
import ProductionForm from "../ProductionForm";
import type { ProductionActionResult } from "../submitProduction";
import type { OnlineInventoryAvailability } from "../onlineInventory";

const fields = [
  ["caliber", "Caliber"],
  ["oal", "OAL"],
  ["bulletWeight", "Bullet weight"],
  ["bulletForm", "Bullet form"],
  ["primer", "Primer"],
  ["powder", "Powder"],
  ["powderWeight", "Powder weight"],
  ["notes", "Notes"],
  ["status", "Status"],
  ["version", "Version"],
  ["updatedAt", "Updated"],
] as const;

export function RecipeProductionError() {
  const { t } = useI18n();
  const routeError = useRouteError();
  const asyncError = useAsyncError();
  const error = routeError ?? asyncError;
  const notFound = (isRouteErrorResponse(error) && error.status === 404)
    || (error instanceof Error && "status" in error && error.status === 404);
  return (
    <div className="state state-error">
      <strong>{t(notFound ? "Recipe not found" : "Could not load this workspace.")}</strong>
      <p>{t(notFound ? "This recipe is no longer active or does not belong to this account." : "The request could not be completed.")}</p>
      <Link className="button button-quiet" to="/ammunition?tab=recipes">{t("Back to recipes")}</Link>
    </div>
  );
}

export default function RecipeProduction() {
  const resources = useLoaderData() as RecipeProductionLoaderResources;
  return (
    <DeferredRoute data={resources.recipe} errorElement={<RecipeProductionError />} fallback={<DetailSkeleton />}>
      {(recipe) => <RecipeProductionContent recipe={recipe} inventory={resources.inventory} />}
    </DeferredRoute>
  );
}

function RecipeProductionContent({
  recipe,
  inventory,
}: {
  recipe: RecipeProduction;
  inventory: RecipeProductionLoaderResources["inventory"];
}) {
  const { t } = useI18n();
  const fetcher = useFetcher<ProductionActionResult>();
  const [qrDataUrl, setQrDataUrl] = useState<string>();
  const [qrError, setQrError] = useState(false);
  const destination = `${window.location.origin}/recipes/${encodeURIComponent(recipe.id)}/produce`;

  useEffect(() => {
    let active = true;
    void QRCode.toDataURL(destination, {
      errorCorrectionLevel: "M",
      margin: 2,
      width: 280,
      color: { dark: "#111111", light: "#ffffff" },
    }).then((dataUrl) => {
      if (active) setQrDataUrl(dataUrl);
    }).catch(() => {
      if (active) setQrError(true);
    });
    return () => {
      active = false;
    };
  }, [destination]);

  const display = (value: string | number | null | undefined) =>
    value === null || value === undefined || value === "" ? t("Not set") : String(value);

  return (
    <div>
      <PageTitle title={recipe.name} subtitle={t("Recipe production")}/>
      <div className="recipe-production-layout">
        <section className="panel recipe-production-card">
          <div className="record-card-top">
            <span className="eyebrow">{recipe.caliber}</span>
            <span className="badge">v{recipe.version}</span>
          </div>
          <dl className="recipe-detail-grid">
            {fields.map(([name, label]) => (
              <div key={name} className={name === "notes" ? "recipe-detail-wide" : undefined}>
                <dt>{t(label)}</dt>
                <dd>{display(recipe[name as keyof WorkshopRecipe] as string | number | null | undefined)}</dd>
              </div>
            ))}
          </dl>
        </section>
        <section className="panel recipe-qr-card">
          <h2>{t("Scan to produce")}</h2>
          <p>{t("Keep this code next to your reloading machine for fast access.")}</p>
          {qrDataUrl && <img className="recipe-qr-image" src={qrDataUrl} alt={t("QR code for this recipe")} />}
          {qrError && <p className="form-error" role="alert">{t("Could not create the QR code.")}</p>}
          {qrDataUrl && (
            <a className="button button-quiet" href={qrDataUrl} download={`${recipe.name}-workshop-qr.png`}>
              {t("Download QR code")}
            </a>
          )}
        </section>
      </div>
      <DeferredRoute data={inventory} fallback={<PanelSkeleton />}>
        {(availability) => availability.inventory
          ? (
            <ProductionForm
              recipes={[recipe]}
              inventory={availability.inventory}
              fixedRecipe={recipe}
              fetcher={fetcher}
            />
          )
          : <InventoryUnavailable availability={availability} />}
      </DeferredRoute>
    </div>
  );
}

function InventoryUnavailable({ availability }: { availability: OnlineInventoryAvailability }) {
  const { t } = useI18n();
  return (
    <section className="panel" role="status">
      <h2>{availability.online ? t("Inventory summary unavailable") : t("Inventory is online-only")}</h2>
      <p>{availability.error ?? t("Reconnect to see current supply stock and finance totals.")}</p>
      <Link to="/inventory">{t("Open inventory")}</Link>
    </section>
  );
}
