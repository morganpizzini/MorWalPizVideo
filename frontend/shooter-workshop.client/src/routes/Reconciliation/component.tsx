import { PageTitle } from "@morwalpiz/layout";
import { useFetcher, useLoaderData } from "react-router";
import type { action } from "./action";
import type { ReconciliationLoaderData } from "./loader";
import { useI18n } from "../../i18n";
import { formatDateTime } from "../../i18n-formatters";

export default function Reconciliation() {
  const { culture, t } = useI18n();
  const { status } = useLoaderData() as ReconciliationLoaderData;
  const fetcher = useFetcher<typeof action>();
  return (
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
              onClick={() => fetcher.submit({}, { method: "post" })}
              disabled={fetcher.state !== "idle"}
            >
              {fetcher.state === "submitting" ? t("Rebuilding…") : t("Rebuild projections")}
            </button>
            {fetcher.state === "idle" && fetcher.data && !fetcher.data.ok && (
              <p className="form-error" role="alert">{fetcher.data.error}</p>
            )}
          </section>
        </div>
  );
}
