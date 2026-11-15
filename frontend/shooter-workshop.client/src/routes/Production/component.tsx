import { PageTitle } from "@morwalpiz/layout";
import { useFetcher, useLoaderData } from "react-router";
import { useI18n } from "../../i18n";
import { localDateInputValue } from "../../i18n-formatters";
import type { action } from "./action";
import type { ProductionLoaderData } from "./loader";

type ProductionFetcher = ReturnType<typeof useFetcher<typeof action>>;

function ActionFeedback({ fetcher }: { fetcher: ProductionFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data) return null;
  return (
    <p className={fetcher.data.ok ? "form-success" : "form-error"} role="alert">
      {fetcher.data.ok ? "Production recorded." : fetcher.data.error}
    </p>
  );
}

export default function Production() {
  const { t } = useI18n();
  const { recipes } = useLoaderData() as ProductionLoaderData;
  const fetcher = useFetcher<typeof action>();

  return (
    <div>
      <PageTitle
        title={t("Production sessions")}
        subtitle={t("Creates a batch.")}
      />
      <fetcher.Form method="post" className="panel form-panel">
        <h2>{t("Record production")}</h2>
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
        <label>
          {t("Notes")}
          <textarea name="notes" />
        </label>
        <ActionFeedback fetcher={fetcher} />
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>
          {fetcher.state !== "idle" ? t("Saving…") : t("Create batch")}
        </button>
      </fetcher.Form>
    </div>
  );
}
