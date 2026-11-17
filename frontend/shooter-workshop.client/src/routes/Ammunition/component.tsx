import { EmptyState, PageTitle, TabbedWorkspace } from "@morwalpiz/layout";
import { useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useI18n } from "../../i18n";
import { formatNumber, localDateInputValue } from "../../i18n-formatters";
import type { action } from "./action";
import type { AmmunitionLoaderData } from "./loader";
import Recipes from "../Recipes/component";

type AmmunitionFetcher = ReturnType<typeof useFetcher<typeof action>>;

function ActionFeedback({ fetcher }: { fetcher: AmmunitionFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data) return null;
  return (
    <p className={fetcher.data.ok ? "form-success" : "form-error"} role="alert">
      {fetcher.data.ok ? "Production recorded." : fetcher.data.error}
    </p>
  );
}

export default function Ammunition() {
  const { culture, t } = useI18n();
  const { recipes, batches } = useLoaderData() as AmmunitionLoaderData;
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get("tab") === "recipes" ? "recipes" : "ammunition";
  const fetcher = useFetcher<typeof action>();

  return (
    <div>
      <PageTitle
        title={t("Ammunition")}
        subtitle={t("Manage ammunition production and recipes.")}
      />
      <TabbedWorkspace
        aria-label={t("Ammunition sections")}
        selectedKey={activeTab}
        onSelectedKeyChange={(key) => setSearchParams(key === "recipes" ? { tab: key } : {})}
        tabs={[
          {
            key: "ammunition",
            label: t("Ammunition"),
            panel: <>
      <fetcher.Form method="post" className="panel form-panel ammunition-production-form">
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
            </>,
          },
          { key: "recipes", label: t("Recipes"), panel: <Recipes embedded /> },
        ]}
      />
    </div>
  );
}
