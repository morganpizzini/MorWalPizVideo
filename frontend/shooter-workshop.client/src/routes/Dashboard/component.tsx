import { Link, useLoaderData, useNavigate, useOutletContext, useSearchParams } from "react-router";
import { useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type {
  WorkshopAnalytics,
  WorkshopAnalyticsBucket,
  WorkshopDashboard,
  WorkshopActivity,
  WorkshopInventory,
} from "@morwalpizvideo/models";
import { ActivityList, DeferredRoute, Stat } from "../shared";
import { ActivitySkeleton, MetricSkeleton, PanelSkeleton } from "../../components/loading/LoadingPrimitives";
import type { DashboardLoaderData } from "./loader";
import { useI18n } from "../../i18n";
import { formatDate, formatNumber } from "../../i18n-formatters";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { readWorkshopPreferences, writeWorkshopPreferences, type WorkshopPersonalGoal } from "../../services/workshopPreferences";
import type { WorkshopShellContext } from "../../layouts/AppShell";

export default function Dashboard() {
  const data = useLoaderData() as DashboardLoaderData;
  const { t } = useI18n();
  const { offline } = useOutletContext<WorkshopShellContext>();
  return (
    <div>
      <PageTitle title={t("Workshop dashboard")} subtitle={t("Account-owned equipment, stock and operation history.")} />
      <section className="panel dashboard-hero">
        <p className="eyebrow">{t("Operational overview")}</p>
        <h2>{t("Every operation remains attributable.")}</h2>
        <p>{t("Firearms, physical components, batches and shooting facts are kept independent by the API.")}</p>
        <section className="dashboard-offline" aria-label={t("Offline readiness")}>
          <div className="dashboard-offline-copy">
            <strong>{offline.effectivelyOffline ? t("Offline workspace") : t("Offline readiness")}</strong>
            <span>{offline.effectivelyOffline
              ? t("Changes are saved on this device. Server session status will be checked when reconnected.")
              : offline.preparedAt
                ? t("This device has a saved Workshop snapshot.")
                : t("Prepare this device while online before going to the range.")}</span>
          </div>
          <div className="dashboard-offline-actions">
            {offline.pending > 0 && <span>{t("{count} pending", { count: offline.pending })}</span>}
            {!offline.effectivelyOffline && (
              <>
                <button
                  type="button"
                  className="button button-quiet dashboard-offline-prepare"
                  disabled={offline.busy || offline.effectivelyOffline}
                  aria-busy={offline.preparing}
                  onClick={offline.prepare}
                >
                  {offline.preparing && <span className="loading-spinner" aria-hidden="true" />}
                  {offline.preparing ? t("Preparing…") : t("Prepare offline")}
                </button>
                {offline.pending > 0 && (
                  <button
                    type="button"
                    className="button button-quiet"
                    disabled={offline.busy || offline.effectivelyOffline}
                    onClick={offline.sync}
                  >
                    {t("Sync now")}
                  </button>
                )}
              </>
            )}
          </div>
        </section>
        <Link className="button button-primary" to="/sessions/shooting">{t("Record a shooting session")}</Link>
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Quick actions")}</h2></div>
        <div className="quick-actions">
          <Link className="quick-action" to="/firearms"><strong>{t("Register firearm")}</strong><span>{t("Keep equipment counters independent.")}</span></Link>
          <Link className="quick-action" to="/ammunition?tab=recipes"><strong>{t("Create recipe")}</strong><span>{t("Capture the immutable recipe snapshot.")}</span></Link>
          <Link className="quick-action" to="/sessions/shooting?tab=maintenance"><strong>{t("Record maintenance")}</strong><span>{t("Keep service history factual and traceable.")}</span></Link>
          <Link className="quick-action" to="/integrity"><strong>{t("Check integrity")}</strong><span>{t("Review reconciliation status.")}</span></Link>
        </div>
      </section>
      <DeferredRoute
        data={data.inventory}
        fallback={<section className="panel" aria-busy="true">{t("Loading inventory summary…")}</section>}
        errorElement={<DashboardInventoryError />}
      >
        {(inventory) => <DashboardInventory inventory={inventory} />}
      </DeferredRoute>
      <DeferredRoute data={Promise.all([data.dashboard, data.history, data.analytics]).then(([dashboard, history, analytics]) => ({ dashboard, history, analytics, days: data.days }))} fallback={<><MetricSkeleton count={4} /><PanelSkeleton /><section className="panel"><ActivitySkeleton rows={4} /></section></>}>
        {(resolved) => <DashboardContent data={resolved} />}
      </DeferredRoute>
    </div>
  );
}

function DashboardInventoryError() {
  const { t } = useI18n();
  return (
    <section className="panel" role="status">
      <div className="section-heading">
        <h2>{t("Inventory summary unavailable")}</h2>
        <Link to="/inventory">{t("Open inventory")}</Link>
      </div>
      <p>{t("This online summary could not be refreshed. Other dashboard data is still available.")}</p>
    </section>
  );
}

function DashboardInventory({ inventory }: { inventory: WorkshopInventory | null }) {
  const { culture, t } = useI18n();
  if (!inventory) {
    return (
      <section className="panel">
        <div className="section-heading">
          <h2>{t("Online inventory")}</h2>
          <Link to="/inventory">{t("Open inventory")}</Link>
        </div>
        <p>{t("Reconnect to see current supply stock and finance totals.")}</p>
      </section>
    );
  }
  const products = new Map(inventory.products.map((product) => [product.id, product]));
  const availableUnits = (type: "Primer" | "Bullet") =>
    inventory.lots.reduce((total, lot) =>
      lot.isEmpty || products.get(lot.productId)?.type !== type
        ? total
        : total + lot.availableQuantity,
    0);
  const openLots = inventory.lots.filter((lot) => !lot.isEmpty && lot.availableQuantity > 0).length;

  return (
    <section className="panel dashboard-inventory">
      <div className="section-heading">
        <h2>{t("Online inventory")}</h2>
        <Link to="/inventory">{t("Open inventory")}</Link>
      </div>
      <p>{t("{products} products · {lots} available lots", {
        products: formatNumber(inventory.products.filter((product) => !product.archived).length, culture),
        lots: formatNumber(openLots, culture),
      })}</p>
      <div className="dashboard-inventory-units">
        <span>{t("Primer units available")} <strong>{formatNumber(availableUnits("Primer"), culture)}</strong></span>
        <span>{t("Bullet units available")} <strong>{formatNumber(availableUnits("Bullet"), culture)}</strong></span>
      </div>
      {inventory.spending.length > 0 && (
        <ul className="inventory-currency-list">
          {inventory.spending.map((entry) => (
            <li key={entry.currency}>
              <span>{t("Recorded purchases")} · {entry.currency}</span>
              <strong>{formatNumber(entry.total, culture)} {entry.currency}</strong>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function DashboardContent({ data }: { data: { dashboard: WorkshopDashboard; history: WorkshopActivity[]; analytics: WorkshopAnalytics; days: 30 | 90 | 365 } }) {
  const { culture, t } = useI18n();
  const { session } = useWorkshopAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const selectedDays = data.days;
  const accountId = session.account?.id;
  const preferences = accountId ? readWorkshopPreferences(accountId) : null;
  const [goal, setGoal] = useState<WorkshopPersonalGoal | null>(() => preferences?.personalGoal ?? null);
  const [goalKind, setGoalKind] = useState<WorkshopPersonalGoal["kind"]>(goal?.kind ?? "shootingRounds");
  const [goalTarget, setGoalTarget] = useState(String(goal?.target ?? ""));
  const setPeriod = (value: string) => {
    const days = Number(value) === 90 || Number(value) === 365 ? Number(value) : 30;
    if (session.account?.id) {
      const preferences = readWorkshopPreferences(session.account.id);
      writeWorkshopPreferences(session.account.id, { ...preferences, dashboardDays: days as 30 | 90 | 365 });
    }
    const next = new URLSearchParams(searchParams);
    next.set("days", String(days));
    navigate(`/?${next.toString()}`);
  };
  const saveGoal = () => {
    const target = Number(goalTarget);
    if (!accountId || !Number.isSafeInteger(target) || target <= 0 || target > 1_000_000_000) return;
    const nextGoal = { kind: goalKind, target };
    const nextPreferences = readWorkshopPreferences(accountId);
    writeWorkshopPreferences(accountId, { ...nextPreferences, personalGoal: nextGoal });
    setGoal(nextGoal);
  };
  const clearGoal = () => {
    if (!accountId) return;
    const nextPreferences = readWorkshopPreferences(accountId);
    writeWorkshopPreferences(accountId, { ...nextPreferences, personalGoal: null });
    setGoal(null);
    setGoalTarget("");
  };
  const goalValue = goal?.kind === "producedRounds" ? data.analytics.producedRounds : data.analytics.shootingRounds;
  const goalPercentage = goal ? Math.min(100, goalValue / goal.target * 100) : 0;

  return (
    <div>
      <div className="dashboard-grid">
        <section className="stats-grid dashboard-stats-grid">
          <Stat label="Active firearms" value={data.dashboard.firearmCount} />
          <Stat label="Firearm rounds" value={formatNumber(data.dashboard.totalFirearmRounds, culture)} />
          <Stat label="Physical components" value={data.dashboard.componentCount} />
          <Stat label="Available stock" value={formatNumber(data.dashboard.availableRounds, culture)} />
        </section>
      </div>
      <section className="panel">
        <div className="section-heading">
          <h2>{t("Recorded activity")}</h2>
          <label>{t("Period")}
            <select value={selectedDays} onChange={(event) => setPeriod(event.target.value)}>
              <option value="30">{t("Last 30 days")}</option>
              <option value="90">{t("Last 90 days")}</option>
              <option value="365">{t("Last year")}</option>
            </select>
          </label>
        </div>
        <div className="stats-grid">
          <Stat label="Shooting rounds" value={formatNumber(data.analytics.shootingRounds, culture)} />
          <Stat label="Shooting sessions" value={data.analytics.shootingSessions} />
          <Stat label="Produced rounds" value={formatNumber(data.analytics.producedRounds, culture)} />
          <Stat label="Untracked rounds" value={formatNumber(data.analytics.untrackedRounds, culture)} />
          <Stat label="Maintenance events" value={data.analytics.maintenanceEvents} />
          <Stat label="Malfunctions" value={data.analytics.malfunctionEvents} />
        </div>
        {data.analytics.projectionState !== "Current" && (
          <p className="status-note mt-2" role="status">{t("Projection state")}: {t(data.analytics.projectionState)}</p>
        )}
        {data.analytics.buckets.length
          ? <DailyActivityTable buckets={data.analytics.buckets} />
          : <p>{t("No recorded activity in this period.")}</p>}
      </section>
      <section className="panel personal-goal">
        <div className="section-heading"><h2>{t("Personal goal")}</h2><span className="status-note">{t("Presentation-only")}</span></div>
        {goal ? (
          <>
            <p>{t(goal.kind === "producedRounds" ? "Produce {target} rounds in this period." : "Shoot {target} rounds in this period.", { target: formatNumber(goal.target, culture) })}</p>
            <div className="goal-track" role="progressbar" aria-valuemin={0} aria-valuemax={goal.target} aria-valuenow={Math.min(goalValue, goal.target)}><span style={{ width: `${goalPercentage}%` }} /></div>
            <div className="section-heading"><strong>{formatNumber(goalValue, culture)} / {formatNumber(goal.target, culture)}</strong><button className="button button-quiet" type="button" onClick={clearGoal}>{t("Remove goal")}</button></div>
          </>
        ) : (
          <div className="goal-form">
            <label>{t("Goal type")}<select value={goalKind} onChange={(event) => setGoalKind(event.target.value as WorkshopPersonalGoal["kind"])}><option value="shootingRounds">{t("Shooting rounds")}</option><option value="producedRounds">{t("Produced rounds")}</option></select></label>
            <label>{t("Target rounds")}<input type="number" min="1" max="1000000000" step="1" value={goalTarget} onChange={(event) => setGoalTarget(event.target.value)} /></label>
            <button className="button button-primary" type="button" onClick={saveGoal}>{t("Set goal")}</button>
          </div>
        )}
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Recent activity")}</h2><Link to="/history">{t("View history")}</Link></div>
        {data.history.length ? <ActivityList compact items={data.history.slice(0, 10)} /> : <EmptyState title={t("No activity yet")} message={t("Your first operation will appear here.")} />}
      </section>
    </div>
  );
}

export function DailyActivityTable({ buckets }: { buckets: WorkshopAnalyticsBucket[] }) {
  const { culture, t } = useI18n();
  return (
    <div className="table-scroll">
      <table>
        <caption>{t("Daily recorded activity")}</caption>
        <thead><tr><th>{t("Date")}</th><th>{t("Rounds")}</th><th>{t("Notes")}</th></tr></thead>
        <tbody>{buckets.map((bucket) => (
          <tr key={bucket.date}>
            <td>{formatDate(bucket.date, culture)}{bucket.shootingSessions > 1 && ` (${formatNumber(bucket.shootingSessions, culture)})`}</td>
            <td>
              <div className="daily-activity-rounds">
                {bucket.producedRounds > 0 && <span className="daily-activity-produced"><span className="visually-hidden">{t("Produced")}: </span>+{formatNumber(bucket.producedRounds, culture)}</span>}
                {bucket.shootingRounds > 0 && <span className="daily-activity-used"><span className="visually-hidden">{t("Used")}: </span>-{formatNumber(bucket.shootingRounds, culture)}</span>}
                {bucket.producedRounds === 0 && bucket.shootingRounds === 0 && "-"}
              </div>
            </td>
            <td>{bucket.notes?.filter((note) => note.trim()).join(" · ") || "-"}</td>
          </tr>
        ))}</tbody>
      </table>
    </div>
  );
}
