import { useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import {
  getWorkshopRecipes,
  recordWorkshopProduction,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { localDateInputValue } from "../../i18n-formatters";
export default function Production() {
  const { t } = useI18n();
  const recipes = useResource(getWorkshopRecipes);
  const [form, setForm] = useState({
    recipeId: "",
    quantityProduced: "0",
    productionDate: localDateInputValue(),
    notes: "",
  });
  return (
    <div>
      <PageTitle
        title={t("Production sessions")}
        subtitle={t("POST /api/v1/workshop/ammunition/production creates a batch.")}
      />
      <ResourceState resource={recipes}>
        {(items) => (
          <WorkshopForm
            title="Record production"
            submitLabel="Create batch"
            onSubmit={async () => {
              await recordWorkshopProduction({
                recipeId: form.recipeId,
                productionDate: form.productionDate,
                quantityProduced: Number(form.quantityProduced),
                notes: form.notes,
                idempotencyKey: crypto.randomUUID(),
              });
            }}
          >
            <label>
              {t("Recipe")}
              <select
                required
                value={form.recipeId}
                onChange={(event) =>
                  setForm({ ...form, recipeId: event.target.value })
                }
              >
                <option value="">{t("Select recipe")}</option>
                {items.map((recipe) => (
                  <option value={recipe.id} key={recipe.id}>
                    {recipe.name} · {recipe.caliber}
                  </option>
                ))}
              </select>
            </label>
            <div className="form-grid">
              <label>
                {t("Quantity")}
                <input
                  type="number"
                  min="1"
                  required
                  value={form.quantityProduced}
                  onChange={(event) =>
                    setForm({ ...form, quantityProduced: event.target.value })
                  }
                />
              </label>
              <label>
                {t("Date")}
                <input
                  type="date"
                  required
                  value={form.productionDate}
                  onChange={(event) =>
                    setForm({ ...form, productionDate: event.target.value })
                  }
                />
              </label>
            </div>
            <label>
              {t("Notes")}
              <textarea
                value={form.notes}
                onChange={(event) =>
                  setForm({ ...form, notes: event.target.value })
                }
              />
            </label>
          </WorkshopForm>
        )}
      </ResourceState>
    </div>
  );
}
