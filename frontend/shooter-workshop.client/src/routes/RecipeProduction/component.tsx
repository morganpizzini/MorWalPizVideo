import { useEffect, useState } from "react";
import QRCode from "qrcode";
import { Link, useAsyncError, useFetcher, useLoaderData, useRouteError, isRouteErrorResponse } from "react-router";
import { PageTitle } from "@morwalpiz/layout";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import type { action } from "./action";
import type { RecipeProductionLoaderData } from "./loader";
import { DeferredRoute, type DeferredRouteData } from "../shared";
import { DetailSkeleton } from "../../components/loading/LoadingPrimitives";

const fields = [
  ["caliber", "Caliber"],
  ["oal", "OAL"],
  ["bulletWeight", "Bullet weight"],
  ["bulletForm", "Bullet form"],
  ["drawing", "Drawing"],
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
  const { data } = useLoaderData() as DeferredRouteData<RecipeProductionLoaderData>;
  return <DeferredRoute data={data} errorElement={<RecipeProductionError />} fallback={<DetailSkeleton />}>{(resolved) => <RecipeProductionContent recipe={resolved} />}</DeferredRoute>;
}

function RecipeProductionContent({ recipe }: { recipe: RecipeProductionLoaderData }) {
  const { t } = useI18n();
  const fetcher = useFetcher<typeof action>();
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
      <section className="panel form-panel recipe-production-form">
        <h2>{t("Record production")}</h2>
        <fetcher.Form method="post">
          <input type="hidden" name="recipeId" value={recipe.id} />
          <div className="form-grid">
            <label>
              {t("Production date")}
              <input name="productionDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} required />
            </label>
            <label>
              {t("Quantity produced")}
              <input name="quantityProduced" type="number" min="1" step="1" required />
            </label>
          </div>
          <label>
            {t("Notes")}
            <textarea name="notes" />
          </label>
          {fetcher.data && !fetcher.data.ok && <p className="form-error" role="alert">{fetcher.data.error}</p>}
          {fetcher.data?.ok && <p className="form-success" role="status">{t("Production recorded.")}</p>}
          <button className="button button-primary" disabled={fetcher.state !== "idle"}>
            {fetcher.state === "idle" ? t("Record production") : t("Saving…")}
          </button>
        </fetcher.Form>
      </section>
    </div>
  );
}
