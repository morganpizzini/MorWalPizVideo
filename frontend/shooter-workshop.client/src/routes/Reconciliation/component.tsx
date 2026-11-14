import { PageTitle } from "@morwalpiz/layout";
import {
  getWorkshopReconciliation,
  rebuildWorkshopProjections,
} from "@morwalpizvideo/services";
import { ResourceState, useResource } from "../shared";
import { useI18n } from "../../i18n";
import { formatDateTime } from "../../i18n-formatters";
export default function Reconciliation() {
  const { culture, t } = useI18n();
  const resource = useResource(getWorkshopReconciliation);
  return (
    <ResourceState resource={resource}>
      {(status) => (
        <div>
          <PageTitle
            title={t("Integrity and reconciliation")}
            subtitle={t("Projection state is returned by the canonical reconciliation endpoint.")}
          />
          <section
            className={`panel projection projection-${status.state.toLowerCase()}`}
          >
            <h2>{t(status.state)}</h2>
            <p>
              {status.mismatchCount
                ? t("{count} mismatches detected.", { count: status.mismatchCount })
                : t("No mismatches reported.")}
            </p>
            <p>{t("Last run: {date}", { date: formatDateTime(status.lastRunAt, culture) })}</p>
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
              {t("Rebuild projections")}
            </button>
          </section>
        </div>
      )}
    </ResourceState>
  );
}
