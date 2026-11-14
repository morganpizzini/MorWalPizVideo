import { Link } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import {
  getWorkshopDashboard,
  getWorkshopHistory,
} from "@morwalpizvideo/services";
import { ErrorState, LoadingState } from "../../components/States";
import { ActivityList, Stat, useResource } from "../shared";
export default function Dashboard() {
  const dashboard = useResource(getWorkshopDashboard);
  const history = useResource(getWorkshopHistory);
  if (dashboard.loading || history.loading) return <LoadingState />;
  const error = dashboard.error ?? history.error;
  if (error) return <ErrorState error={error} />;
  const overview = dashboard.value;
  if (!overview)
    return (
      <ErrorState error={new Error("The dashboard response was empty.")} />
    );
  return (
    <div>
      <PageTitle
        title="Workshop dashboard"
        subtitle="Account-owned equipment, stock and operation history."
      />
      <div className="dashboard-grid">
        <section className="panel dashboard-hero">
          <p className="eyebrow">Operational overview</p>
          <h2>Every operation remains attributable.</h2>
          <p>
            Firearms, physical components, batches and shooting facts are kept
            independent by the API.
          </p>
          <Link className="button button-primary" to="/sessions/shooting">
            Record a shooting session
          </Link>
        </section>
        <section className="stats-grid">
          <Stat label="Active firearms" value={overview.firearmCount} />
          <Stat
            label="Firearm rounds"
            value={overview.totalFirearmRounds.toLocaleString()}
          />
          <Stat label="Physical components" value={overview.componentCount} />
          <Stat
            label="Available stock"
            value={overview.availableRounds.toLocaleString()}
          />
        </section>
      </div>
      <section className="panel">
        <div className="section-heading">
          <h2>Quick actions</h2>
        </div>
        <div className="quick-actions">
          <Link className="quick-action" to="/firearms">
            <strong>Register firearm</strong>
            <span>Keep equipment counters independent.</span>
          </Link>
          <Link className="quick-action" to="/recipes">
            <strong>Create recipe</strong>
            <span>Capture the immutable recipe snapshot.</span>
          </Link>
          <Link className="quick-action" to="/production">
            <strong>Record production</strong>
            <span>Create stock from a recipe.</span>
          </Link>
          <Link className="quick-action" to="/integrity">
            <strong>Check integrity</strong>
            <span>Review reconciliation status.</span>
          </Link>
        </div>
      </section>
      <section className="panel">
        <div className="section-heading">
          <h2>Recent activity</h2>
          <Link to="/history">View history</Link>
        </div>
        {history.value?.length ? (
          <ActivityList items={history.value.slice(0, 10)} />
        ) : (
          <EmptyState
            title="No activity yet"
            message="Your first operation will appear here."
          />
        )}
      </section>
    </div>
  );
}
