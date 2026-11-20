import { EmptyState } from "@morwalpiz/layout";
import type { WorkshopAmmunitionBatch, WorkshopRecipe } from "@morwalpizvideo/models";
import { useFetcher } from "react-router";
import { useEffect, useRef } from "react";
import { useI18n } from "../../i18n";
import { formatNumber, localDateInputValue } from "../../i18n-formatters";
import type { action } from "./action";

type AmmunitionFetcher = ReturnType<typeof useFetcher<typeof action>>;

function ActionFeedback({ fetcher }: { fetcher: AmmunitionFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data) return null;
  return (
    <p className={fetcher.data.ok ? "form-success" : "form-error"} role="alert">
      {fetcher.data.ok ? "Production recorded." : fetcher.data.error}
    </p>
  );
}

export default function AmmunitionPanel({
  recipes,
  batches,
  fetcher,
}: {
  recipes: WorkshopRecipe[];
  batches: WorkshopAmmunitionBatch[];
  fetcher: AmmunitionFetcher;
}) {
  const { culture, t } = useI18n();
  const formRef = useRef<HTMLFormElement>(null);

  useEffect(() => {
    if (fetcher.state === "idle" && fetcher.data?.ok) {
      formRef.current?.reset();
    }
  }, [fetcher.data, fetcher.state]);

  return (
    <>
      <fetcher.Form ref={formRef} method="post" className="panel form-panel ammunition-production-form">
        <h2>{t("Record production")}</h2>
        <div className="ammunition-production-grid">
          <div className="ammunition-production-primary">
            <label>
              {t("Recipe")}
              <select name="recipeId" required defaultValue="">
                <option value="">{t("Select recipe")}</option>
                {recipes.map((recipe) => (
                  <option value={recipe.id} key={recipe.id}>
                    {recipe.name} · {recipe.caliber}
                  </option>
                ))}
              </select>
            </label>
            <div className="form-grid">
              <label>
                {t("Quantity")}
                <input type="number" min="1" required name="quantityProduced" defaultValue="0" />
              </label>
              <label>
                {t("Date")}
                <input type="date" required name="productionDate" defaultValue={localDateInputValue()} />
              </label>
            </div>
          </div>
          <label className="ammunition-production-notes">
            {t("Notes")}
            <textarea name="notes" />
          </label>
        </div>
        <ActionFeedback fetcher={fetcher} />
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>
          {fetcher.state !== "idle" ? t("Saving…") : t("Create batch")}
        </button>
      </fetcher.Form>
      <section className="panel">
        <h2>{t("Produced batches")}</h2>
        {batches.length ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{t("Batch")}</th>
                  <th>{t("Caliber")}</th>
                  <th>{t("Produced")}</th>
                  <th>{t("On hand")}</th>
                  <th>{t("Revision")}</th>
                </tr>
              </thead>
              <tbody>
                {batches.map((batch) => (
                  <tr key={batch.id}>
                    <td>{batch.id}</td>
                    <td>{batch.recipeSnapshot.caliber}</td>
                    <td>{formatNumber(batch.producedQuantity, culture)}</td>
                    <td>
                      <strong>{formatNumber(batch.onHandQuantity, culture)}</strong>
                    </td>
                    <td>{batch.revision}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState title={t("No ammunition batches")} />
        )}
      </section>
    </>
  );
}
