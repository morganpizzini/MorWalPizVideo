import { useEffect, useRef, useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  CreateWorkshopRecipeRequest,
  WorkshopRecipe,
} from "@morwalpizvideo/models";
import {
  createWorkshopRecipe,
  getWorkshopRecipes,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
export default function Recipes() {
  const { t } = useI18n();
  const resource = useResource(getWorkshopRecipes);
  const [showForm, setShowForm] = useState(false);
  const [selectedRecipe, setSelectedRecipe] = useState<WorkshopRecipe | null>(
    null,
  );
  return (
    <ResourceState resource={resource}>
      {(items) => (
        <div>
          <PageTitle
            title={t("Ammunition recipes")}
            subtitle={t("Recipe fields match the server snapshot contract.")}
          />
          <button
            className="button button-primary"
            onClick={() => setShowForm(!showForm)}
          >
            + {t("New recipe")}
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
                <article
                  className="record-card"
                  key={recipe.id}
                  role="button"
                  tabIndex={0}
                  aria-label={`${recipe.name} ${t("recipe details")}`}
                  onClick={() => setSelectedRecipe(recipe)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" || event.key === " ") {
                      event.preventDefault();
                      setSelectedRecipe(recipe);
                    }
                  }}
                >
                  <div className="record-card-top">
                    <span className="eyebrow">{recipe.caliber}</span>
                    <span className="badge">v{recipe.version}</span>
                  </div>
                  <h2>{recipe.name}</h2>
                  <p>
                    {recipe.primer || t("Primer not set")} ·{" "}
                    {recipe.oal || t("OAL not set")}
                  </p>
                  <p>
                    {recipe.powder || t("Powder not set")}
                    {recipe.powderWeight != null
                      ? ` · ${recipe.powderWeight}`
                      : ""}{" "}
                    · {t(recipe.status)}
                  </p>
                </article>
              ))}
            </div>
          ) : (
            <EmptyState title={t("No recipes yet")} />
          )}
          {selectedRecipe ? (
            <RecipeDetails
              recipe={selectedRecipe}
              onClose={() => setSelectedRecipe(null)}
            />
          ) : null}
        </div>
      )}
    </ResourceState>
  );
}

function RecipeForm({ onSaved }: { onSaved: () => void }) {
  const { t } = useI18n();
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
          {t("Name")}
          <input
            required
            value={form.name}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
          />
        </label>
        <label>
          {t("Caliber")}
          <input
            required
            value={form.caliber}
            onChange={(event) =>
              setForm({ ...form, caliber: event.target.value })
            }
          />
        </label>
        <label>
          {t("Primer")}
          <input
            value={form.primer ?? ""}
            onChange={(event) =>
              setForm({ ...form, primer: event.target.value })
            }
          />
        </label>
        <label>
          {t("OAL")}
          <input
            type="number"
            min="0"
            step="0.01"
            inputMode="decimal"
            value={form.oal ?? ""}
            onChange={(event) => setForm({ ...form, oal: event.target.value })}
          />
        </label>
        <label>
          {t("Powder")}
          <input
            value={form.powder ?? ""}
            onChange={(event) =>
              setForm({ ...form, powder: event.target.value })
            }
          />
        </label>
        <label>
          {t("Powder weight")}
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
        {t("Notes")}
        <textarea
          value={form.notes ?? ""}
          onChange={(event) => setForm({ ...form, notes: event.target.value })}
        />
      </label>
    </WorkshopForm>
  );
}

function RecipeDetails({
  recipe,
  onClose,
}: {
  recipe: WorkshopRecipe;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    previousFocusRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
        return;
      }

      if (event.key !== "Tab") return;
      const dialog = document.getElementById("recipe-details-dialog");
      if (!dialog) return;
      const focusable = Array.from(
        dialog.querySelectorAll<HTMLElement>(
          'button:not([disabled]), [tabindex]:not([tabindex="-1"])',
        ),
      );
      if (!focusable.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [onClose]);

  const display = (value: string | number | null | undefined) =>
    value === null || value === undefined || value === ""
      ? t("Not set")
      : String(value);

  return (
    <div
      className="feedback-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        id="recipe-details-dialog"
        className="feedback-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="recipe-details-title"
      >
        <div className="section-heading">
          <h2 id="recipe-details-title">{recipe.name}</h2>
          <button
            ref={closeButtonRef}
            type="button"
            className="button button-quiet"
            onClick={onClose}
          >
            {t("Close")}
          </button>
        </div>
        <dl>
          <dt>{t("Caliber")}</dt>
          <dd>{display(recipe.caliber)}</dd>
          <dt>{t("Primer")}</dt>
          <dd>{display(recipe.primer)}</dd>
          <dt>{t("OAL")}</dt>
          <dd>{display(recipe.oal)}</dd>
          <dt>{t("Bullet weight")}</dt>
          <dd>{display(recipe.bulletWeight)}</dd>
          <dt>{t("Bullet form")}</dt>
          <dd>{display(recipe.bulletForm)}</dd>
          <dt>{t("Drawing")}</dt>
          <dd>{display(recipe.drawing)}</dd>
          <dt>{t("Powder")}</dt>
          <dd>{display(recipe.powder)}</dd>
          <dt>{t("Powder weight")}</dt>
          <dd>{display(recipe.powderWeight)}</dd>
          <dt>{t("Status")}</dt>
          <dd>{t(recipe.status)}</dd>
          <dt>{t("Version")}</dt>
          <dd>{recipe.version}</dd>
          <dt>{t("Notes")}</dt>
          <dd>{display(recipe.notes)}</dd>
        </dl>
      </section>
    </div>
  );
}
