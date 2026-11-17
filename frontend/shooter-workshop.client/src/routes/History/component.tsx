import { PageTitle, EmptyState } from "@morwalpiz/layout";
import { Link, useLoaderData, useNavigate, useSearchParams } from "react-router";
import { useEffect } from "react";
import { ActivityList, DeferredRoute, type DeferredRouteData } from "../shared";
import { useI18n } from "../../i18n";
import { useWorkshopAuth } from "../../auth/AuthProvider";
import { readWorkshopPreferences, writeWorkshopPreferences } from "../../services/workshopPreferences";
import { frontendEndpoints } from "@morwalpizvideo/services";
import type { HistoryLoaderData } from "./loader";
import { ActivitySkeleton } from "../../components/loading/LoadingPrimitives";

const historyTypes = [
  ["", "All activity"],
  ["FirearmCreated", "Firearms"],
  ["ComponentCreated", "Components"],
  ["MaintenanceRecorded", "Maintenance"],
  ["RecipeCreated", "Recipes"],
  ["ProductionRecorded", "Production"],
  ["ShootingRecorded", "Shooting"],
] as const;

export default function History() {
  const { data } = useLoaderData() as DeferredRouteData<HistoryLoaderData>;
  const { t } = useI18n();
  return (
    <div>
      <div className="section-heading">
        <PageTitle title={t("History")} subtitle={t("Account-filtered chronological activity from the controller.")} />
        <a className="button button-quiet" href={`/${frontendEndpoints.WORKSHOP_REPORT}?format=csv`} download>{t("Download 30-day CSV")}</a>
      </div>
      <DeferredRoute data={data} fallback={<section className="panel"><ActivitySkeleton rows={8} /></section>}>{(resolved) => <HistoryContent data={resolved} />}</DeferredRoute>
    </div>
  );
}

function HistoryContent({ data }: { data: HistoryLoaderData }) {
  const { t } = useI18n();
  const { session } = useWorkshopAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { items, page, pageSize, type } = data;
  const savedType = session.account?.id ? readWorkshopPreferences(session.account.id).historyOperationType : "";
  const selectedType = type || savedType;
  useEffect(() => {
    if (!type && savedType) {
      const next = new URLSearchParams(searchParams);
      next.set("type", savedType);
      navigate(`/history?${next.toString()}`, { replace: true });
    }
  }, [navigate, savedType, searchParams, type]);
  const changeType = (nextType: string) => {
    if (session.account?.id) {
      const preferences = readWorkshopPreferences(session.account.id);
      writeWorkshopPreferences(session.account.id, { ...preferences, historyOperationType: nextType });
    }
    const next = new URLSearchParams(searchParams);
    next.delete("page");
    if (nextType) next.set("type", nextType);
    else next.delete("type");
    navigate(`/history${next.toString() ? `?${next.toString()}` : ""}`);
  };
  const makePageUrl = (nextPage: number) => {
    const next = new URLSearchParams(searchParams);
    next.set("page", String(nextPage));
    return `/history?${next.toString()}`;
  };
  return (
    <div>
      <section className="panel history-toolbar">
        <label>
          {t("Activity view")}
          <select value={selectedType} onChange={(event) => changeType(event.target.value)}>
            {historyTypes.map(([value, label]) => <option value={value} key={value}>{t(label)}</option>)}
          </select>
        </label>
        <p>{t("This view changes presentation only; account ownership and query limits remain server-controlled.")}</p>
      </section>
      {items.length ? (
        <section className="panel"><ActivityList items={items} /></section>
      ) : <EmptyState title={t("No history yet")} />}
      {(page > 1 || items.length === pageSize) ? (
        <nav className="record-actions" aria-label={t("History pages")}>
          {page > 1 && <Link className="button button-quiet" to={makePageUrl(page - 1)}>{t("Previous")}</Link>}
          <span>{t("Page {value}", { value: page })}</span>
          {items.length === pageSize && <Link className="button button-quiet" to={makePageUrl(page + 1)}>{t("Next")}</Link>}
        </nav>
      ) : null}
    </div>
  );
}
