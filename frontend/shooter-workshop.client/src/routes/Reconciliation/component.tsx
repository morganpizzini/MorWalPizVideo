import { PageTitle } from "@morwalpiz/layout";
import {
  getWorkshopReconciliation,
  rebuildWorkshopProjections,
} from "@morwalpizvideo/services";
import { ResourceState, useResource } from "../shared";
export default function Reconciliation() {
  const resource = useResource(getWorkshopReconciliation);
  return (
    <ResourceState resource={resource}>
      {(status) => (
        <div>
          <PageTitle
            title="Integrity and reconciliation"
            subtitle="Projection state is returned by the canonical reconciliation endpoint."
          />
          <section
            className={`panel projection projection-${status.state.toLowerCase()}`}
          >
            <h2>{status.state}</h2>
            <p>
              {status.mismatchCount
                ? `${status.mismatchCount} mismatches detected.`
                : "No mismatches reported."}
            </p>
            <p>Last run: {new Date(status.lastRunAt).toLocaleString()}</p>
            {status.mismatches.length > 0 && (
              <ul>
                {status.mismatches.map((mismatch) => (
                  <li key={mismatch}>{mismatch}</li>
                ))}
              </ul>
            )}
            <button
              className="button button-primary"
              onClick={() =>
                void rebuildWorkshopProjections().then(resource.reload)
              }
            >
              Rebuild projections
            </button>
          </section>
        </div>
      )}
    </ResourceState>
  );
}
