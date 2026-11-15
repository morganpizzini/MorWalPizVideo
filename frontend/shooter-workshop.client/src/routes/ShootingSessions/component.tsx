import { useMemo, useState } from "react";
import { PageTitle } from "@morwalpiz/layout";
import type {
  WorkshopConsumptionRequest,
  WorkshopFirearm,
  WorkshopRecipe,
  WorkshopShootingSessionRecord,
} from "@morwalpizvideo/models";
import {
  correctWorkshopShooting,
  getWorkshopFirearms,
  getWorkshopRecipes,
  getWorkshopShootingSessions,
  recordWorkshopShooting,
} from "@morwalpizvideo/services";
import { WorkshopForm } from "../../components/WorkshopForm";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";

type RecipeRow = WorkshopConsumptionRequest & { key: string };

export default function ShootingSessions() {
  const { t } = useI18n();
  const resource = useResource(() =>
    Promise.all([getWorkshopFirearms(), getWorkshopRecipes(), getWorkshopShootingSessions()]),
  );
  const [firearmId, setFirearmId] = useState("");
  const [untracked, setUntracked] = useState("0");
  const [records, setRecords] = useState<RecipeRow[]>([]);
  const trackedRounds = records.reduce((sum, record) => sum + Math.max(0, record.quantity), 0);
  const totalRounds = trackedRounds + Math.max(0, Number(untracked) || 0);

  return (
    <div>
      <PageTitle title={t("Shooting sessions")} subtitle={t("Record sessions and correct them inline without rewriting history.")} />
      <ResourceState resource={resource}>
        {([firearms, recipes, sessions]) => (
          <>
            <WorkshopForm
              title="Record shooting"
              submitLabel="Record session"
              onSubmit={async () => {
                await recordWorkshopShooting({
                  firearmId,
                  date: new Date().toISOString(),
                  totalRounds,
                  untrackedAmmunition: Number(untracked),
                  ammunitionRecords: records
                    .filter((record) => record.recipeId && record.quantity > 0)
                    .map(({ key: _key, ...record }) => record),
                  usageSegments: [],
                  idempotencyKey: crypto.randomUUID(),
                });
                resource.reload();
              }}
            >
              <label>
                {t("Firearm")}
                <select required value={firearmId} onChange={(event) => setFirearmId(event.target.value)}>
                  <option value="">{t("Select firearm")}</option>
                  {firearms.filter((firearm) => !firearm.archived).map((firearm) => (
                    <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>
                  ))}
                </select>
              </label>
              <div className="form-grid">
                <label>{t("Untracked ammunition")}<input type="number" min="0" required value={untracked} onChange={(event) => setUntracked(event.target.value)} /></label>
                <div className="calculated-field"><span>{t("Total rounds")}</span><strong>{totalRounds}</strong></div>
              </div>
              <AmmunitionFields records={records} recipes={recipes} onChange={setRecords} />
            </WorkshopForm>
            <ShootingHistory sessions={sessions} firearms={firearms} recipes={recipes} onReload={resource.reload} />
          </>
        )}
      </ResourceState>
    </div>
  );
}

function AmmunitionFields({
  records,
  recipes,
  onChange,
}: {
  records: RecipeRow[];
  recipes: WorkshopRecipe[];
  onChange: (records: RecipeRow[]) => void;
}) {
  const { t } = useI18n();
  return (
    <fieldset>
      <legend>{t("Ammunition records")}</legend>
      {records.map((record, index) => (
        <div className="shooting-ammunition-row" key={record.key}>
          <label>{t("Recipe")}
            <select required value={record.recipeId} onChange={(event) => {
              const next = [...records]; next[index] = { ...record, recipeId: event.target.value }; onChange(next);
            }}>
              <option value="">{t("Select recipe")}</option>
              {recipes.filter((recipe) => recipe.status.toLowerCase() !== "archived").map((recipe) => <option value={recipe.id} key={recipe.id}>{recipe.name} · {recipe.caliber}</option>)}
            </select>
          </label>
          <label>{t("Quantity")}<input type="number" min="1" required value={record.quantity || ""} onChange={(event) => {
            const next = [...records]; next[index] = { ...record, quantity: Number(event.target.value) }; onChange(next);
          }} /></label>
          <button type="button" className="button button-quiet" onClick={() => onChange(records.filter((_, itemIndex) => itemIndex !== index))}>{t("Remove")}</button>
        </div>
      ))}
      <button type="button" className="button button-quiet" onClick={() => onChange([...records, { key: crypto.randomUUID(), recipeId: "", quantity: 0 }])}>+ {t("Ammunition record")}</button>
    </fieldset>
  );
}

function ShootingHistory({
  sessions,
  firearms,
  recipes,
  onReload,
}: {
  sessions: WorkshopShootingSessionRecord[];
  firearms: WorkshopFirearm[];
  recipes: WorkshopRecipe[];
  onReload: () => void;
}) {
  const { t } = useI18n();
  const labels = useMemo(() => new Map(firearms.map((firearm) => [firearm.id, firearm.nickname || `${firearm.manufacturer} ${firearm.model}`])), [firearms]);
  return (
    <section className="panel">
      <div className="section-heading"><h2>{t("Recent shooting sessions")}</h2></div>
      {sessions.length === 0 ? <p>{t("No shooting sessions recorded.")}</p> : sessions.map((session) => (
        <ShootingRow key={session.session.id} record={session} recipes={recipes} firearmLabel={labels.get(session.session.firearmId) ?? t("Firearm")} onReload={onReload} />
      ))}
    </section>
  );
}

function ShootingRow({
  record,
  recipes,
  firearmLabel,
  onReload,
}: {
  record: WorkshopShootingSessionRecord;
  recipes: WorkshopRecipe[];
  firearmLabel: string;
  onReload: () => void;
}) {
  const { t } = useI18n();
  const { session } = record;
  const [editing, setEditing] = useState(false);
  const [quantities, setQuantities] = useState<Record<string, number>>(() =>
    session.consumptionRecords.reduce<Record<string, number>>((result, item) => {
      if (item.recipeId) result[item.recipeId] = (result[item.recipeId] ?? 0) + item.quantity;
      return result;
    }, {}),
  );
  const [error, setError] = useState("");
  const editable = Boolean(session.operationId) && !record.superseded && !session.correctionOfId;
  const trackedTotal = Object.values(quantities).reduce((sum, quantity) => sum + Math.max(0, quantity), 0);
  const correctedTotal = trackedTotal + session.untrackedAmmunition;
  const save = async (nextQuantities = quantities) => {
    if (!session.operationId) return;
    const nextTrackedTotal = Object.values(nextQuantities).reduce((sum, quantity) => sum + Math.max(0, quantity), 0);
    const nextTotal = nextTrackedTotal + session.untrackedAmmunition;
    if (nextTotal === 0 && !window.confirm(t("Delete this shooting session? Its audit history will be retained."))) return;
    try {
      setError("");
      await correctWorkshopShooting({
        originalOperationId: session.operationId,
        replacementShooting: nextTotal === 0 ? null : {
          firearmId: session.firearmId,
          date: session.date,
          totalRounds: nextTotal,
          untrackedAmmunition: session.untrackedAmmunition,
          ammunitionRecords: Object.entries(nextQuantities)
            .filter(([, quantity]) => quantity > 0)
            .map(([recipeId, quantity]) => ({ recipeId, quantity })),
          usageSegments: [],
          idempotencyKey: crypto.randomUUID(),
        },
        idempotencyKey: crypto.randomUUID(),
      });
      setEditing(false);
      onReload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : t("The request could not be completed."));
    }

  };
  return (
    <article className="detail-list-item">
      <div><strong>{firearmLabel}</strong><span>{new Date(session.date).toLocaleDateString()}</span></div>
      {editing ? (
        <fieldset>
          <legend>{t("Correct ammunition quantities")}</legend>
          {Object.entries(quantities).map(([recipeId, quantity]) => (
            <label key={recipeId}>
              {recipes.find((recipe) => recipe.id === recipeId)?.name ?? recipeId}
              <input
                type="number"
                min="0"
                value={quantity}
                onChange={(event) => setQuantities((current) => ({ ...current, [recipeId]: Number(event.target.value) }))}
              />
            </label>
          ))}
          <p>{t("Untracked ammunition")}: {session.untrackedAmmunition} ({t("unchanged")})</p>
          <strong>{t("Total rounds")}: {correctedTotal}</strong>
        </fieldset>
      ) : <strong>{session.totalRounds} {t("rounds")}</strong>}
      <div className="button-row">
        {editing ? <>
          <button type="button" className="button" onClick={() => void save()}>{t("Save")}</button>
          <button type="button" className="button button-quiet" onClick={() => setEditing(false)}>{t("Cancel")}</button>
        </> : editable && <>
          <button type="button" className="button button-quiet" onClick={() => {
            setQuantities(session.consumptionRecords.reduce<Record<string, number>>((result, item) => {
              if (item.recipeId) result[item.recipeId] = (result[item.recipeId] ?? 0) + item.quantity;
              return result;
            }, {}));
            setEditing(true);
          }}>{t("Edit")}</button>
          <button type="button" className="button button-quiet" onClick={() => {
            setQuantities({});
            void save({});
          }}>{t("Delete")}</button>
        </>}
      </div>
      {error && <p role="alert">{error}</p>}
    </article>
  );
}
