import { Link } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import {
  getWorkshopDashboard,
  getWorkshopHistory,
} from "@morwalpizvideo/services";
import { ErrorState, LoadingState } from "../../components/States";
import { ActivityList, Stat, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatNumber } from "../../i18n-formatters";
export default function Dashboard() {
  const { culture, t } = useI18n();
  const dashboard = useResource(getWorkshopDashboard);
  const history = useResource(getWorkshopHistory);
  if (dashboard.loading || history.loading) return <LoadingState />;
  const error = dashboard.error ?? history.error;
  if (error) return <ErrorState error={error} />;
  const overview = dashboard.value;
  if (!overview)
    return (
      <ErrorState error={new Error(t("The dashboard response was empty."))} />
    );
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
          <Stat label="Active firearms" value={overview.firearmCount} />
          <Stat
            label="Firearm rounds"
            value={formatNumber(overview.totalFirearmRounds, culture)}
          />
          <Stat label="Physical components" value={overview.componentCount} />
          <Stat
            label="Available stock"
            value={formatNumber(overview.availableRounds, culture)}
          />
        </section>
      </div>
      <section className="panel">
        <div className="section-heading">
          <h2>{t("Quick actions")}</h2>
        </div>
        <div className="quick-actions">
          <Link className="quick-action" to="/firearms">
            <strong>{t("Register firearm")}</strong>
            <span>{t("Keep equipment counters independent.")}</span>
          </Link>
          <Link className="quick-action" to="/recipes">
            <strong>{t("Create recipe")}</strong>
            <span>{t("Capture the immutable recipe snapshot.")}</span>
          </Link>
          <Link className="quick-action" to="/ammunition">
            <strong>{t("Record production")}</strong>
            <span>{t("Create stock from a recipe.")}</span>
          </Link>
          <Link className="quick-action" to="/integrity">
            <strong>{t("Check integrity")}</strong>
            <span>{t("Review reconciliation status.")}</span>
          </Link>
        </div>
      </section>
      <section className="panel">
        <div className="section-heading">
          <h2>{t("Recent activity")}</h2>
          <Link to="/history">{t("View history")}</Link>
        </div>
        {history.value?.length ? (
          <ActivityList items={history.value.slice(0, 10)} />
        ) : (
          <EmptyState
            title={t("No activity yet")}
            message={t("Your first operation will appear here.")}
          />
        )}
      </section>
    </div>
  );
}
