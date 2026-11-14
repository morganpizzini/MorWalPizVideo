import { useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import {
  getWorkshopRecipes,
  recordWorkshopProduction,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Production() {
  const recipes = useResource(getWorkshopRecipes);
  const [form, setForm] = useState({
    recipeId: "",
    quantityProduced: "0",
    productionDate: new Date().toISOString().slice(0, 10),
    notes: "",
  });
  return (
    <div>
      <PageTitle
        title="Production sessions"
        subtitle="POST /api/v1/workshop/ammunition/production creates a batch."
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
              Recipe
              <select
                required
                value={form.recipeId}
                onChange={(event) =>
                  setForm({ ...form, recipeId: event.target.value })
                }
              >
                <option value="">Select recipe</option>
                {items.map((recipe) => (
                  <option value={recipe.id} key={recipe.id}>
                    {recipe.name} · {recipe.caliber}
                  </option>
                ))}
              </select>
            </label>
            <div className="form-grid">
              <label>
                Quantity
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
                Date
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
              Notes
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
