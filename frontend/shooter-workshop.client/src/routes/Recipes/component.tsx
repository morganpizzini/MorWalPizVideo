import { useEffect, useState } from "react";
import { Dialog, EmptyState, PageTitle } from "@morwalpiz/layout";
import type { WorkshopRecipe } from "@morwalpizvideo/models";
import { Link, useFetcher } from "react-router";
import { useI18n } from "../../i18n";
import type { action } from "./action";

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

export default function Recipes({
  recipes,
  embedded = false,
}: {
  recipes: WorkshopRecipe[];
  embedded?: boolean;
}) {
  const { t } = useI18n();
  const [showForm, setShowForm] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  return (
    <div>
      {!embedded && <PageTitle
        title={t("Ammunition recipes")}
        subtitle={t("Recipe fields match the server snapshot contract.")}
      />}
      <div className="section-heading workspace-action-heading">
        <span />
        <button className="button button-primary" type="button" onClick={() => setShowForm(true)}>
          + {t("New recipe")}
        </button>
      </div>
      <Dialog open={showForm} title={t("New recipe")} closeLabel={t("Close")} onClose={() => setShowForm(false)}>
        <RecipeForm onSaved={() => setShowForm(false)} onCancel={() => setShowForm(false)} />
      </Dialog>
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
  if (!recipe) return null;
  const display = (value: string | number | null | undefined) =>
    value === null || value === undefined || value === "" ? t("Not set") : String(value);

  return (
    <Dialog open title={recipe.name} closeLabel={t("Close")} onClose={onClose} className="recipe-dialog">
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
              {recipe.status === "Active" && (
                <Link
                  className="button button-primary"
                  to={`/recipes/${encodeURIComponent(recipe.id)}/produce`}
                  onClick={onClose}
                >
                  {t("Open production")}
                </Link>
              )}
            </div>
          </>
        )}
    </Dialog>
  );
}
