import { Link, useLoaderData, useNavigate, useSearchParams } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { WorkshopAnalytics, WorkshopDashboard, WorkshopActivity } from "@morwalpizvideo/models";
import { ActivityList, Stat } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { readWorkshopPreferences, writeWorkshopPreferences } from "../../services/workshopPreferences";

interface DashboardLoaderData {
  dashboard: WorkshopDashboard;
  history: WorkshopActivity[];
  analytics: WorkshopAnalytics;
  days: 30 | 90 | 365;
}

export default function Dashboard() {
  const { culture, t } = useI18n();
  const { session } = useWorkshopAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const data = useLoaderData() as DashboardLoaderData;
  const selectedDays = data.days;
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

  return (
    <div>
      <PageTitle
        title={t("Workshop dashboard")}
        subtitle={t("Account-owned equipment, stock and operation history.")}
      />
      <div className="dashboard-grid">
        <section className="panel dashboard-hero">
          <p className="eyebrow">{t("Operational overview")}</p>
          <h2>{t("Every operation remains attributable.")}</h2>
          <p>{t("Firearms, physical components, batches and shooting facts are kept independent by the API.")}</p>
          <Link className="button button-primary" to="/sessions/shooting">
            {t("Record a shooting session")}
          </Link>
        </section>
        <section className="stats-grid">
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
          <p className="status-note" role="status">{t("Projection state")}: {t(data.analytics.projectionState)}</p>
        )}
        {data.analytics.buckets.length ? (
          <div className="table-scroll">
            <table>
              <caption>{t("Daily recorded activity")}</caption>
              <thead><tr><th>{t("Date")}</th><th>{t("Shooting rounds")}</th><th>{t("Produced rounds")}</th><th>{t("Sessions")}</th></tr></thead>
              <tbody>{data.analytics.buckets.map((bucket) => (
                <tr key={bucket.date}>
                  <td>{new Intl.DateTimeFormat(culture, { dateStyle: "medium" }).format(new Date(bucket.date))}</td>
                  <td>{formatNumber(bucket.shootingRounds, culture)}</td>
                  <td>{formatNumber(bucket.producedRounds, culture)}</td>
                  <td>{bucket.shootingSessions}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        ) : <p>{t("No recorded activity in this period.")}</p>}
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Quick actions")}</h2></div>
        <div className="quick-actions">
          <Link className="quick-action" to="/firearms"><strong>{t("Register firearm")}</strong><span>{t("Keep equipment counters independent.")}</span></Link>
          <Link className="quick-action" to="/recipes"><strong>{t("Create recipe")}</strong><span>{t("Capture the immutable recipe snapshot.")}</span></Link>
          <Link className="quick-action" to="/ammunition"><strong>{t("Record production")}</strong><span>{t("Create stock from a recipe.")}</span></Link>
          <Link className="quick-action" to="/maintenance"><strong>{t("Record maintenance")}</strong><span>{t("Keep service history factual and traceable.")}</span></Link>
          <Link className="quick-action" to="/integrity"><strong>{t("Check integrity")}</strong><span>{t("Review reconciliation status.")}</span></Link>
        </div>
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Recent activity")}</h2><Link to="/history">{t("View history")}</Link></div>
        {data.history.length ? <ActivityList items={data.history.slice(0, 10)} /> : <EmptyState title={t("No activity yet")} message={t("Your first operation will appear here.")} />}
      </section>
    </div>
  );
}
