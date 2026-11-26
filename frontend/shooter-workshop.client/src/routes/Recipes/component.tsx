import { useEffect, useState } from "react";
import { Dialog, EmptyState, PageTitle } from "@morwalpiz/layout";
import type {
  WorkshopInventory,
  WorkshopRecipe,
  WorkshopRecipeJournalEntry,
} from "@morwalpizvideo/models";
import { Link, useFetcher, useSearchParams } from "react-router";
import { useI18n } from "../../i18n";
import { DeferredRoute } from "../shared";
import type { action } from "./action";

type RecipeFetcher = ReturnType<typeof useFetcher<typeof action>>;
type RecipeJournalLoaderData = {
  recipeId: string;
  entries: WorkshopRecipeJournalEntry[];
} | null;

const fields = [
  ["name", "Name", "text", true],
  ["caliber", "Caliber", "text", true],
  ["oal", "OAL", "text", false],
  ["bulletWeight", "Bullet weight", "number", false],
  ["bulletForm", "Bullet form", "text", false],
  ["powderWeight", "Powder weight", "number", false],
] as const;

export default function Recipes({
  recipes,
  inventory,
  inventoryLoading = false,
  journal,
  embedded = false,
}: {
  recipes: WorkshopRecipe[];
  inventory: WorkshopInventory | null;
  inventoryLoading?: boolean;
  journal?: Promise<RecipeJournalLoaderData>;
  embedded?: boolean;
}) {
  const { t } = useI18n();
  const [showForm, setShowForm] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedId = searchParams.get("recipeId");

  useEffect(() => {
    if (selectedId && !recipes.some((recipe) => recipe.id === selectedId)) {
      setSearchParams((current) => {
        const next = new URLSearchParams(current);
        next.delete("recipeId");
        return next;
      }, { replace: true, preventScrollReset: true });
    }
  }, [recipes, selectedId, setSearchParams]);

  const selectRecipe = (recipeId: string | null) => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      if (recipeId) next.set("recipeId", recipeId);
      else next.delete("recipeId");
      return next;
    }, { preventScrollReset: true });
  };

  return (
    <div>
      {!embedded && <PageTitle
        title={t("Ammunition recipes")}
        subtitle={t("Recipe fields match the server snapshot contract.")}
      />}
      <div className="section-heading workspace-action-heading">
        <span />
        <button className="button button-primary" type="button" disabled={!inventory} onClick={() => setShowForm(true)}>
          + {t("New recipe")}
        </button>
      </div>
      {inventory ? (
        <Dialog open={showForm} title={t("New recipe")} closeLabel={t("Close")} onClose={() => setShowForm(false)}>
          <RecipeForm inventory={inventory} onSaved={() => setShowForm(false)} onCancel={() => setShowForm(false)} />
        </Dialog>
      ) : (
        <p className="status-note">
          {t(inventoryLoading
            ? "Loading online inventory..."
            : "Inventory must be available online to edit recipe products.")}
        </p>
      )}
      {recipes.length ? (
        <div className="card-grid mt-3">
          {recipes.map((recipe) => (
            <article
              className="record-card"
              key={recipe.id}
              role="button"
              tabIndex={0}
              aria-label={`${recipe.name} ${t("recipe details")}`}
              onClick={() => selectRecipe(recipe.id)}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  selectRecipe(recipe.id);
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
          inventory={inventory}
          journal={journal}
          onClose={() => selectRecipe(null)}
        />
      )}
    </div>
  );
}

function ActionFeedback({ fetcher }: { fetcher: RecipeFetcher }) {
  if (fetcher.state !== "idle" || !fetcher.data || fetcher.data.ok) return null;
  const { t } = useI18n();
  return (
    <p className="form-error" role="alert">
      {t(fetcher.data.error)}
    </p>
  );
}

function RecipeForm({
  recipe,
  inventory,
  onSaved,
  onCancel,
}: {
  recipe?: WorkshopRecipe;
  inventory: WorkshopInventory;
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
      <div className="form-grid">
        <label>
          {t("Primer product")}
          <select name="primerProductId" required defaultValue={recipe?.primerProductId ?? ""}>
            <option value="">{t("Select primer product")}</option>
            {inventory.products.filter((product) => product.type === "Primer" && !product.archived).map((product) => (
              <option value={product.id} key={product.id}>
                {product.name}{product.brand ? ` · ${product.brand}` : ""}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t("Powder product")}
          <select name="powderProductId" required defaultValue={recipe?.powderProductId ?? ""}>
            <option value="">{t("Select powder product")}</option>
            {inventory.products.filter((product) => product.type === "Powder" && !product.archived).map((product) => (
              <option value={product.id} key={product.id}>
                {product.name}{product.brand ? ` · ${product.brand}` : ""}
              </option>
            ))}
          </select>
        </label>
      </div>
      {(!inventory.products.some((product) => product.type === "Primer" && !product.archived)
        || !inventory.products.some((product) => product.type === "Powder" && !product.archived)) && (
        <p className="status-note">
          {t("Create active primer and powder products before saving this recipe.")}
          {" "}
          <Link to="/inventory">{t("Open inventory")}</Link>
        </p>
      )}
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
  inventory,
  journal,
  onClose,
}: {
  recipe: WorkshopRecipe | null;
  inventory: WorkshopInventory | null;
  journal?: Promise<RecipeJournalLoaderData>;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const [editing, setEditing] = useState(false);
  const [addingJournalEntry, setAddingJournalEntry] = useState(false);
  if (!recipe) return null;
  const display = (value: string | number | null | undefined) =>
    value === null || value === undefined || value === "" ? t("Not set") : String(value);

  return (
    <Dialog open title={recipe.name} closeLabel={t("Close")} onClose={onClose} className="recipe-dialog">
      {editing ? inventory ? (
          <RecipeForm
            recipe={recipe}
            inventory={inventory}
            onSaved={() => setEditing(false)}
            onCancel={() => setEditing(false)}
          />
      ) : (
        <p className="status-note">{t("Inventory must be available online to edit recipe products.")}</p>
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
                <dt>{t("Primer")}</dt>
                <dd>{display(recipe.primer)}</dd>
              </div>
              <div>
                <dt>{t("Powder")}</dt>
                <dd>{display(recipe.powder)}</dd>
              </div>
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
                  disabled={!inventory}
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
            <section className="recipe-journal" aria-labelledby="recipe-journal-heading">
              <div className="recipe-journal-heading">
                <div>
                  <h3 id="recipe-journal-heading">{t("Recipe journal")}</h3>
                  <p>{t("Keep a manual record of conditions and observations for this recipe.")}</p>
                </div>
                <button
                  type="button"
                  className="button button-primary"
                  disabled={recipe.status !== "Active"}
                  onClick={() => setAddingJournalEntry(true)}
                >
                  {t("Add journal entry")}
                </button>
              </div>
              <p className="status-note">{t("Journal entries are available online only.")}</p>
              {recipe.status !== "Active" && (
                <p className="status-note">{t("Archived recipes are read-only.")}</p>
              )}
              {addingJournalEntry && recipe.status === "Active" && (
                <RecipeJournalForm
                  recipeId={recipe.id}
                  onSaved={() => setAddingJournalEntry(false)}
                  onCancel={() => setAddingJournalEntry(false)}
                />
              )}
              {journal && (
                <DeferredRoute
                  data={journal}
                  fallback={<p className="status-note" role="status">{t("Loading recipe journal...")}</p>}
                >
                  {(data) => data?.recipeId === recipe.id ? (
                    <RecipeJournalList entries={data.entries} />
                  ) : (
                    <p className="status-note">{t("Recipe journal is unavailable for this recipe.")}</p>
                  )}
                </DeferredRoute>
              )}
            </section>
          </>
      )}
    </Dialog>
  );
}

function RecipeJournalList({ entries }: { entries: WorkshopRecipeJournalEntry[] }) {
  const { culture, t } = useI18n();
  const display = (value: string | null | undefined) => value || t("Not set");
  if (!entries.length) return <p className="status-note">{t("No journal entries yet")}</p>;

  return (
    <div
      className="recipe-journal-table-wrap"
      role="region"
      aria-label={t("Recipe journal entries")}
      tabIndex={0}
    >
      <table className="recipe-journal-table">
        <caption className="visually-hidden">{t("Recipe journal entries")}</caption>
        <thead>
          <tr>
            <th scope="col">{t("Date")}</th>
            <th scope="col">{t("Time of day")}</th>
            <th scope="col">{t("Weather")}</th>
            <th scope="col">{t("Temperature")}</th>
            <th scope="col">{t("Power factor")}</th>
            <th scope="col">{t("Notes")}</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry) => (
            <tr key={entry.id}>
              <td>
                <time dateTime={entry.entryDate}>
                  {new Intl.DateTimeFormat(culture, {
                    dateStyle: "medium",
                    timeZone: "UTC",
                  }).format(new Date(`${entry.entryDate}T00:00:00Z`))}
                </time>
              </td>
              <td>{entry.timeOfDay ? t(entry.timeOfDay) : t("Not set")}</td>
              <td>{display(entry.weather)}</td>
              <td>{entry.temperature
                ? `${entry.temperature} ${entry.temperatureUnit ?? ""}`.trim()
                : t("Not set")}</td>
              <td>{display(entry.powerFactor)}</td>
              <td>{display(entry.notes)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RecipeJournalForm({
  recipeId,
  onSaved,
  onCancel,
}: {
  recipeId: string;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const { t } = useI18n();
  const fetcher = useFetcher<typeof action>();
  const [idempotencyKey] = useState(() => crypto.randomUUID());

  useEffect(() => {
    if (fetcher.state === "idle" && fetcher.data?.ok) onSaved();
  }, [fetcher.data, fetcher.state, onSaved]);

  return (
    <fetcher.Form method="post" className="panel recipe-form recipe-journal-form">
      <input type="hidden" name="intent" value="journal-create" />
      <input type="hidden" name="recipeId" value={recipeId} />
      <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
      <h4>{t("Add journal entry")}</h4>
      <div className="form-grid">
        <label>
          {t("Entry date")}
          <input name="entryDate" type="date" required />
        </label>
        <label>
          {t("Time of day")}
          <select name="timeOfDay" defaultValue="">
            <option value="">{t("Select time of day")}</option>
            <option value="Morning">{t("Morning")}</option>
            <option value="Noon">{t("Noon")}</option>
            <option value="Afternoon">{t("Afternoon")}</option>
            <option value="Evening">{t("Evening")}</option>
          </select>
        </label>
        <label>
          {t("Weather")}
          <input name="weather" type="text" maxLength={100} />
        </label>
        <label>
          {t("Temperature")}
          <input
            name="temperature"
            type="text"
            inputMode="decimal"
            maxLength={32}
            pattern="[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)"
          />
        </label>
        <label>
          {t("Temperature unit")}
          <select name="temperatureUnit" defaultValue="">
            <option value="">{t("Select unit")}</option>
            <option value="C">{t("Celsius (C)")}</option>
            <option value="F">{t("Fahrenheit (F)")}</option>
          </select>
        </label>
        <label>
          {t("Power factor")}
          <input
            name="powerFactor"
            type="text"
            inputMode="decimal"
            maxLength={32}
            pattern="[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)"
          />
        </label>
      </div>
      <label>
        {t("Notes")}
        <textarea name="notes" maxLength={2000} />
      </label>
      <ActionFeedback fetcher={fetcher} />
      <div className="feedback-actions">
        <button className="button button-quiet" type="button" onClick={onCancel}>
          {t("Cancel")}
        </button>
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>
          {fetcher.state !== "idle" ? t("Saving…") : t("Save entry")}
        </button>
      </div>
    </fetcher.Form>
  );
}
