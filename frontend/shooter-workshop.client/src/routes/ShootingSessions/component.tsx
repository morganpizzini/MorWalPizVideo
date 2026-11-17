import { useEffect, useMemo, useRef, useState } from "react";
import { useFetcher, useLoaderData, useSearchParams } from "react-router";
import { Dialog, PageTitle, TabbedWorkspace } from "@morwalpiz/layout";
import type {
  WorkshopConsumptionRequest,
  WorkshopFirearm,
  WorkshopRecipe,
  WorkshopShootingSessionRecord,
} from "@morwalpizvideo/models";
import type { action } from "./action";
import { useI18n } from "../../i18n";
import { formatDateTime, localDateTimeInputValue } from "../../i18n-formatters";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { readWorkshopPreferences, writeWorkshopPreferences } from "../../services/workshopPreferences";
import Maintenance from "../Maintenance/component";

type RecipeRow = WorkshopConsumptionRequest & { key: string };

export default function ShootingSessions() {
  const { t } = useI18n();
  const { session } = useWorkshopAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const [activeTab, setActiveTab] = useState(searchParams.get("tab") === "maintenance" ? "maintenance" : "shooting");
  const fetcher = useFetcher<typeof action>();
  const { firearms, recipes, sessions } = useLoaderData() as {
    firearms: WorkshopFirearm[];
    recipes: WorkshopRecipe[];
    sessions: WorkshopShootingSessionRecord[];
  };
  const [firearmId, setFirearmId] = useState(searchParams.get("firearmId") ?? "");
  const [effectiveAt, setEffectiveAt] = useState(localDateTimeInputValue());
  const [notes, setNotes] = useState("");
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [untracked, setUntracked] = useState("0");
  const [records, setRecords] = useState<RecipeRow[]>([]);
  const [draftNotice, setDraftNotice] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const processedResult = useRef<unknown>(null);
  const trackedRounds = records.reduce((sum, record) => sum + Math.max(0, record.quantity), 0);
  const totalRounds = trackedRounds + Math.max(0, Number(untracked) || 0);
  useEffect(() => {
    if (fetcher.state !== "idle" || !fetcher.data?.ok || !("intent" in fetcher.data) || fetcher.data.intent !== "record" ||
        fetcher.data === processedResult.current) return;
    processedResult.current = fetcher.data;
    if (session.account?.id && firearmId && records[0]?.recipeId) {
      const preferences = readWorkshopPreferences(session.account.id);
      writeWorkshopPreferences(session.account.id, {
        ...preferences,
        lastRecipeByFirearm: { ...preferences.lastRecipeByFirearm, [firearmId]: records[0].recipeId },
      });
    }
    setIdempotencyKey(crypto.randomUUID());
    setNotes("");
    setRecords([]);
    setUntracked("0");
    setEffectiveAt(localDateTimeInputValue());
    setDraftNotice(false);
    setShowForm(false);
  }, [fetcher.state, fetcher.data, session.account?.id, firearmId, records]);

  const repeatSession = (record: WorkshopShootingSessionRecord) => {
    setFirearmId(record.session.firearmId);
    setEffectiveAt(localDateTimeInputValue());
    setNotes(record.session.notes ?? "");
    setUntracked(String(record.session.untrackedAmmunition));
    setRecords(record.session.consumptionRecords
      .filter((item) => item.recipeId && item.quantity > 0)
      .map((item) => ({ key: crypto.randomUUID(), recipeId: item.recipeId ?? "", quantity: item.quantity })));
    setIdempotencyKey(crypto.randomUUID());
    setDraftNotice(true);
    setActiveTab("shooting");
    setShowForm(true);
  };

  return (
    <div>
      <PageTitle title={t("Shooting sessions")} subtitle={t("Record shooting sessions and maintain equipment history.")} />
      <TabbedWorkspace
        aria-label={t("Shooting workspace sections")}
        selectedKey={activeTab}
        onSelectedKeyChange={(key) => {
          setActiveTab(key);
          setSearchParams(key === "maintenance" ? { tab: key } : {});
        }}
        tabs={[
          {
            key: "shooting",
            label: t("Shooting sessions"),
            panel: <>
      <div className="section-heading workspace-action-heading">
        <span />
        <button className="button button-primary" type="button" onClick={() => setShowForm(true)}>+ {t("Record shooting")}</button>
      </div>
      <Dialog open={showForm} title={t("Record shooting")} closeLabel={t("Close")} onClose={() => setShowForm(false)}>
      <fetcher.Form method="post" className="panel form-panel shooting-sessions-form">
        <input type="hidden" name="intent" value="record" />
        <input type="hidden" name="totalRounds" value={totalRounds} />
        <input type="hidden" name="idempotencyKey" value={idempotencyKey} />
        <input type="hidden" name="ammunitionRecords" value={JSON.stringify(records
          .filter((record) => record.recipeId && record.quantity > 0)
          .map(({ key: _key, ...record }) => record))} />
        <input type="hidden" name="usageSegments" value="[]" />
        <label>
          {t("Firearm")}
          <select required name="firearmId" value={firearmId} onChange={(event) => setFirearmId(event.target.value)}>
            <option value="">{t("Select firearm")}</option>
            {firearms.filter((firearm) => !firearm.archived).map((firearm) => (
              <option value={firearm.id} key={firearm.id}>{firearm.nickname || `${firearm.manufacturer} ${firearm.model}`}</option>
            ))}
          </select>
        </label>
        <div className="form-grid">
          <label>{t("Effective date and time")}<input name="date" type="datetime-local" required value={effectiveAt} onChange={(event) => setEffectiveAt(event.target.value)} /></label>
          <label>{t("Untracked ammunition")}<input name="untrackedAmmunition" type="number" min="0" required value={untracked} onChange={(event) => setUntracked(event.target.value)} /></label>
          <div className="calculated-field"><span>{t("Total rounds")}</span><strong>{totalRounds}</strong></div>
        </div>
        <label>{t("Notes")}<input name="notes" value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <AmmunitionFields records={records} recipes={recipes} onChange={setRecords} />
        {draftNotice && <p className="status-note" role="status">{t("Repeated session loaded as a new unsaved draft.")}</p>}
        {fetcher.state === "idle" && fetcher.data && !fetcher.data.ok && (
          <p className="form-error" role="alert">{fetcher.data.error}</p>
        )}
        <button className="button button-primary" disabled={fetcher.state !== "idle"}>
          {fetcher.state === "submitting" ? t("Saving…") : t("Record session")}
        </button>
      </fetcher.Form>
      </Dialog>
      <ShootingHistory sessions={sessions} firearms={firearms} recipes={recipes} onRepeat={repeatSession} />
            </>,
          },
          { key: "maintenance", label: t("Maintenance"), panel: <Maintenance embedded /> },
        ]}
      />
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
              {recipes.filter((recipe) => !isArchivedRecipe(recipe.status)).map((recipe) => <option value={recipe.id} key={recipe.id}>{recipe.name} · {recipe.caliber}</option>)}
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

function isArchivedRecipe(status: WorkshopRecipe["status"]) {
  const normalizedStatus = String(status).toLowerCase();
  return normalizedStatus === "archived" || normalizedStatus === "1";
}

function ShootingHistory({
  sessions,
  firearms,
  recipes,
  onRepeat,
}: {
  sessions: WorkshopShootingSessionRecord[];
  firearms: WorkshopFirearm[];
  recipes: WorkshopRecipe[];
  onRepeat: (record: WorkshopShootingSessionRecord) => void;
}) {
  const { t } = useI18n();
  const labels = useMemo(() => new Map(firearms.map((firearm) => [firearm.id, firearm.nickname || `${firearm.manufacturer} ${firearm.model}`])), [firearms]);
  return (
    <section className="panel">
      <div className="section-heading"><h2>{t("Recent shooting sessions")}</h2></div>
      {sessions.length === 0 ? <p>{t("No shooting sessions recorded.")}</p> : sessions.map((session) => (
        <ShootingRow key={session.session.id} record={session} recipes={recipes} firearmLabel={labels.get(session.session.firearmId) ?? t("Firearm")} onRepeat={onRepeat} />
      ))}
    </section>
  );
}

function ShootingRow({
  record,
  recipes,
  firearmLabel,
  onRepeat,
}: {
  record: WorkshopShootingSessionRecord;
  recipes: WorkshopRecipe[];
  firearmLabel: string;
  onRepeat: (record: WorkshopShootingSessionRecord) => void;
}) {
  const { culture, t } = useI18n();
  const { session } = record;
  const fetcher = useFetcher<typeof action>();
  const [editing, setEditing] = useState(false);
  const [quantities, setQuantities] = useState<Record<string, number>>(() =>
    session.consumptionRecords.reduce<Record<string, number>>((result, item) => {
      if (item.recipeId) result[item.recipeId] = (result[item.recipeId] ?? 0) + item.quantity;
      return result;
    }, {}),
  );
  const [error, setError] = useState("");
  useEffect(() => {
    if (fetcher.state !== "idle" || !fetcher.data) return;
    if (fetcher.data.ok) setEditing(false);
    else setError(fetcher.data.error);
  }, [fetcher.state, fetcher.data]);
  const editable = Boolean(session.operationId) && !record.superseded && !session.correctionOfId;
  const trackedTotal = Object.values(quantities).reduce((sum, quantity) => sum + Math.max(0, quantity), 0);
  const correctedTotal = trackedTotal + session.untrackedAmmunition;
  const save = (nextQuantities = quantities) => {
    if (!session.operationId) return;
    const nextTrackedTotal = Object.values(nextQuantities).reduce((sum, quantity) => sum + Math.max(0, quantity), 0);
    const nextTotal = nextTrackedTotal + session.untrackedAmmunition;
    if (nextTotal === 0 && !window.confirm(t("Delete this shooting session? Its audit history will be retained."))) return;
    setError("");
    fetcher.submit({
      intent: "correct",
      originalOperationId: session.operationId,
      replacementShooting: nextTotal === 0 ? "" : JSON.stringify({
        firearmId: session.firearmId,
        date: session.date,
        totalRounds: nextTotal,
        untrackedAmmunition: session.untrackedAmmunition,
        ammunitionRecords: Object.entries(nextQuantities)
          .filter(([, quantity]) => quantity > 0)
          .map(([recipeId, quantity]) => ({ recipeId, quantity })),
        usageSegments: [],
        idempotencyKey: crypto.randomUUID(),
      }),
      idempotencyKey: crypto.randomUUID(),
    }, { method: "post" });
  };
  return (
    <article className="detail-list-item">
      <div><strong>{firearmLabel}</strong>&nbsp;<span>{formatDateTime(session.date, culture)}</span></div>
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
      <div className="button-row mt-2">
        <button type="button" className="button button-quiet me-2" onClick={() => onRepeat(record)}>{t("Repeat as draft")}</button>
        {editing ? <>
          <button type="button" className="button me-2" disabled={fetcher.state !== "idle"} onClick={() => save()}>{t("Save")}</button>
          <button type="button" className="button button-quiet me-2" onClick={() => setEditing(false)}>{t("Cancel")}</button>
        </> : editable && <>
            <button type="button" className="button button-quiet me-2" onClick={() => {
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
