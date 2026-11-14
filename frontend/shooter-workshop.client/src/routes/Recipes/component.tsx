import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { CreateWorkshopRecipeRequest } from "@morwalpizvideo/models";
import {
  createWorkshopRecipe,
  getWorkshopRecipes,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
export default function Recipes() {
  const resource = useResource(getWorkshopRecipes);
  const [showForm, setShowForm] = useState(false);
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title="Ammunition recipes"
            subtitle="Recipe fields match the server snapshot contract."
          />
          <button
            className="button button-primary"
            onClick={() => setShowForm(!showForm)}
          >
            + New recipe
          </button>
          {showForm && (
            <RecipeForm
              onSaved={() => {
                setShowForm(false);
                resource.reload();
              }}
            />
          )}
          {items.length ? (
            <div className="card-grid">
              {items.map((recipe) => (
                <article className="record-card" key={recipe.id}>
                  <div className="record-card-top">
                    <span className="eyebrow">{recipe.caliber}</span>
                    <span className="badge">v{recipe.version}</span>
                  </div>
                  <h2>{recipe.name}</h2>
                  <p>
                    {recipe.powder ?? "Powder not set"} · {recipe.status}
                  </p>
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title="No recipes yet" />
          )}
        </div>
      )}
    </ResourceState>
  );
}

function RecipeForm({ onSaved }: { onSaved: () => void }) {
  const [form, setForm] = useState<CreateWorkshopRecipeRequest>({
    name: "",
    caliber: "",
    oal: "",
    bulletWeight: null,
    bulletForm: "",
    drawing: "",
    primer: "",
    powder: "",
    powderWeight: null,
    notes: "",
  });
  return (
    <WorkshopForm
      title="New recipe"
      submitLabel="Save recipe"
      onSubmit={async () => {
        await createWorkshopRecipe(form);
        onSaved();
      }}
    >
      <div className="form-grid">
        <label>
          Name
          <input
            required
            value={form.name}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
          />
        </label>
        <label>
          Caliber
          <input
            required
            value={form.caliber}
            onChange={(event) =>
              setForm({ ...form, caliber: event.target.value })
            }
          />
        </label>
        <label>
          Powder
          <input
            value={form.powder ?? ""}
            onChange={(event) =>
              setForm({ ...form, powder: event.target.value })
            }
          />
        </label>
        <label>
          Powder weight
          <input
            type="number"
            value={form.powderWeight ?? ""}
            onChange={(event) =>
              setForm({
                ...form,
                powderWeight: event.target.value
                  ? Number(event.target.value)
                  : null,
              })
            }
          />
        </label>
      </div>
      <label>
        Notes
        <textarea
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
    </WorkshopForm>
  );
}
