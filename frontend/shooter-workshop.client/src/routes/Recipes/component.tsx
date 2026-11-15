import { useEffect, useRef, useState } from "react";
import { EmptyState, PageTitle } from "@morwalpiz/layout";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import { useFetcher, useLoaderData } from "react-router";
import { useI18n } from "../../i18n";
import type { action } from "./action";
import type { RecipesLoaderData } from "./loader";

type RecipeFetcher = ReturnType<typeof useFetcher<typeof action>>;

const fields = [
  ["name", "Name", "text", true],
  ["caliber", "Caliber", "text", true],
  ["oal", "OAL", "text", false],
  ["bulletWeight", "Bullet weight", "number", false],
  ["bulletForm", "Bullet form", "text", false],
  ["drawing", "Drawing", "text", false],
  ["primer", "Primer", "text", false],
  ["powder", "Powder", "text", false],
  ["powderWeight", "Powder weight", "number", false],
] as const;

export default function Recipes() {
  const { t } = useI18n();
  const { recipes } = useLoaderData() as RecipesLoaderData;
  const [showForm, setShowForm] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  return (
    <div>
      <PageTitle
        title={t("Ammunition recipes")}
        subtitle={t("Recipe fields match the server snapshot contract.")}
      />
      <button
        className="button button-primary"
        type="button"
        onClick={() => setShowForm((value) => !value)}
      >
        + {t("New recipe")}
      </button>
      {showForm && <RecipeForm onSaved={() => setShowForm(false)} />}
      {recipes.length ? (
        <div className="card-grid mt-3">
          {recipes.map((recipe) => (
            <article
              className="record-card"
              key={recipe.id}
              role="button"
              tabIndex={0}
              aria-label={`${recipe.name} ${t("recipe details")}`}
              onClick={() => setSelectedId(recipe.id)}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  setSelectedId(recipe.id);
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
                {recipe.powderWeight != null ? ` · ${recipe.powderWeight}` : ""} ·{" "}
                {t(recipe.status)}
              </p>
            </article>
          ))}
        </div>
      ) : (
        <EmptyState title={t("No recipes yet")} />
      )}
      {selectedId && (
        <RecipeDetails
          recipe={recipes.find((item) => item.id === selectedId) ?? null}
          onClose={() => setSelectedId(null)}
        />
      )}
    </div>
  );
}

function ActionFeedback({ fetcher }: { fetcher: RecipeFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data || fetcher.data.ok) return null;
  return (
    <p className="form-error" role="alert">
      {fetcher.data.error}
    </p>
  );
}

function RecipeForm({
  recipe,
  onSaved,
  onCancel,
}: {
  recipe?: WorkshopRecipe;
  onSaved?: () => void;
  onCancel?: () => void;
}) {
  const { t } = useI18n();
  const fetcher = useFetcher<typeof action>();
  const editing = Boolean(recipe);

  useEffect(() => {
    if (fetcher.state === "idle" && fetcher.data?.ok) onSaved?.();
  }, [fetcher.data, fetcher.state, onSaved]);

  return (
    <fetcher.Form method="post" className="panel form-panel recipe-form">
      <input type="hidden" name="intent" value={editing ? "update" : "create"} />
      {recipe && (
        <>
          <input type="hidden" name="recipeId" value={recipe.id} />
          <input type="hidden" name="version" value={recipe.version} />
        </>
      )}
      <h2>{t(editing ? "Edit recipe" : "New recipe")}</h2>
      <div className="form-grid">
        {fields.map(([name, label, type, required]) => (
          <label key={name}>
            {t(label)}
            <input
              name={name}
              type={type}
              required={required}
              min={type === "number" ? "0" : undefined}
              step={type === "number" ? "any" : undefined}
              defaultValue={recipe?.[name] ?? ""}
            />
          </label>
        ))}
      </div>
      <label>
        {t("Notes")}
        <textarea name="notes" defaultValue={recipe?.notes ?? ""} />
      </label>
      <ActionFeedback fetcher={fetcher} />
      <div className="feedback-actions">
        {onCancel && (
          <button className="button button-quiet" type="button" onClick={onCancel}>
            {t("Cancel")}
          </button>
        )}
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>
          {fetcher.state !== "idle"
            ? t("Saving…")
            : t(editing ? "Save changes" : "Save recipe")}
        </button>
      </div>
    </fetcher.Form>
  );
}

function RecipeDetails({
  recipe,
  onClose,
}: {
  recipe: WorkshopRecipe | null;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const [editing, setEditing] = useState(false);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    previousFocusRef.current =
      document.activeElement instanceof HTMLElement ? document.activeElement : null;
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
          'button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
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

  if (!recipe) return null;
  const display = (value: string | number | null | undefined) =>
    value === null || value === undefined || value === "" ? t("Not set") : String(value);

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
        className="feedback-dialog recipe-dialog"
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
        {editing ? (
          <RecipeForm
            recipe={recipe}
            onSaved={() => setEditing(false)}
            onCancel={() => setEditing(false)}
          />
        ) : (
          <>
            <dl className="recipe-detail-grid">
              {fields.map(([name, label]) => (
                <div key={name}>
                  <dt>{t(label)}</dt>
                  <dd>{display(recipe[name])}</dd>
                </div>
              ))}
              <div>
                <dt>{t("Status")}</dt>
                <dd>{t(recipe.status)}</dd>
              </div>
              <div>
                <dt>{t("Version")}</dt>
                <dd>{recipe.version}</dd>
              </div>
              <div>
                <dt>{t("Updated")}</dt>
                <dd>{display(recipe.updatedAt)}</dd>
              </div>
              <div className="recipe-detail-wide">
                <dt>{t("Notes")}</dt>
                <dd>{display(recipe.notes)}</dd>
              </div>
            </dl>
            <div className="feedback-actions">
              {recipe.status === "Active" && (
                <button
                  type="button"
                  className="button button-primary"
                  onClick={() => setEditing(true)}
                >
                  {t("Edit")}
                </button>
              )}
            </div>
          </>
        )}
      </section>
    </div>
  );
}
