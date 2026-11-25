import { Link, useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useEffect, useRef, useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { WorkshopComponentCondition } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { formatDate, formatNumber, localDateTimeInputValue } from "../../i18n-formatters";
import { DeferredRoute, Stat, type DeferredRouteData } from "../shared";
import type { action } from "./action";
import type { FirearmDetailLoaderData } from "./loader";
import { DetailSkeleton } from "../../components/loading/LoadingPrimitives";

export default function FirearmDetail() {
  const { data } = useLoaderData() as DeferredRouteData<FirearmDetailLoaderData>;
  const { t } = useI18n();
  return (
    <div className="firearm-detail">
      <Link className="back-link" to="/firearms">← {t("Firearms")}</Link>
      <DeferredRoute data={data} fallback={<DetailSkeleton />}>{(resolved) => <FirearmDetailContent detail={resolved} />}</DeferredRoute>
    </div>
  );
}

function FirearmDetailContent({ detail }: { detail: FirearmDetailLoaderData }) {
  const { culture, t } = useI18n();
  const [searchParams] = useSearchParams();
  const [historyView, setHistoryView] = useState<"chart" | "table">("chart");
  const maintenanceFetcher = useFetcher<typeof action>();
  const [maintenanceKey, setMaintenanceKey] = useState(() => crypto.randomUUID());
  const processedMaintenance = useRef<unknown>(null);
  useEffect(() => {
    if (maintenanceFetcher.state !== "idle" || !maintenanceFetcher.data || !("ok" in maintenanceFetcher.data) ||
      !maintenanceFetcher.data.ok || maintenanceFetcher.data === processedMaintenance.current) return;
    processedMaintenance.current = maintenanceFetcher.data;
    setMaintenanceKey(crypto.randomUUID());
  }, [maintenanceFetcher.state, maintenanceFetcher.data]);
  const maintenanceResult = maintenanceFetcher.data && "intent" in maintenanceFetcher.data &&
    maintenanceFetcher.data.intent === "maintenance" ? maintenanceFetcher.data : null;
  if (!detail?.firearm) {
    return <EmptyState title={t("Firearm not found")} message={t("The record may be archived or belong to another account.")} />;
  }
  const firearm = detail.firearm;
  const title = firearm.nickname || `${firearm.manufacturer} ${firearm.model}`;
  const points = detail.roundHistory;
  const maxRounds = Math.max(...points.map((point) => point.cumulativeRounds), firearm.totalRounds, 1);
  const maxComponentRounds = Math.max(1, ...detail.components.map((item) => item.component.totalRounds));
  const projectionMessage = detail.projectionState === "Current" ? null
    : detail.projectionState === "Pending" ? "Round totals are being reconciled. Some values may be temporarily outdated."
      : detail.projectionState === "Inconsistent" ? "Round totals do not match the recorded ledger. Reconciliation is needed."
        : "Round-total status is unavailable.";
  return (
    <div>
      <PageTitle title={title} subtitle={`${firearm.caliber}${firearm.category ? ` · ${firearm.category}` : ""}`} />
      <div className="stats-grid mb-3">
        <Stat label="Total rounds" value={formatNumber(firearm.totalRounds, culture)} />
        <Stat label="Status" value={t(firearm.archived ? "archived" : "active")} />
        <Stat label="Malfunctions" value={detail.malfunctionCount} />
        <Stat label="Installed components" value={detail.components.length} />
      </div>
      {projectionMessage && <p className="firearm-projection-status" role="status">{t(projectionMessage)}</p>}
      <section className="panel">
        <div className="section-heading"><h2>{t("Installed components")}</h2><Link className="button button-quiet" to={`/firearms?tab=inventory&firearmId=${encodeURIComponent(firearm.id)}`}>{t("Install, swap, or detach a component")}</Link></div>
        {detail.components.length === 0 ? <p>{t("No components installed.")}</p> : (
          <>
            <p className="component-comparison-note">{t("Usage bars compare installed components; service intervals are shown separately.")}</p>
            <div className="component-usage-list">{detail.components.map((item) => <ComponentRow key={item.installation.id} item={item} maxRounds={maxComponentRounds} />)}</div>
          </>
        )}
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Component installation history")}</h2></div>
        {detail.installationHistory.items.length === 0 ? <p>{t("No component installations recorded.")}</p> : (
          <div className="table-wrap">
            <table className="installation-history-table">
              <thead><tr><th>{t("Component")}</th><th>{t("Installed")}</th><th>{t("Removed")}</th></tr></thead>
              <tbody>{detail.installationHistory.items.map(({ component, definition, installation }) => (
                <tr key={installation.id}>
                  <td><strong>{definition.name}</strong>{component.identificationNote && <small>{component.identificationNote}</small>}</td>
                  <td>{formatDate(installation.installedAt, culture)}</td>
                  <td>{installation.removedAt ? formatDate(installation.removedAt, culture) : "-"}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
        <HistoryPager kind="installation" page={detail.installationHistory.page} hasMore={detail.installationHistory.hasMore} searchParams={searchParams} />
      </section>
      {!firearm.archived && <section className="panel firearm-actions">
        <div className="section-heading"><h2>{t("Record activity")}</h2><form method="post"><input type="hidden" name="intent" value="archive" /><input type="hidden" name="revision" value={firearm.revision} /><button className="button button-quiet" type="submit">{t("Archive firearm")}</button></form></div>
        <div className="form-grid">
          <div className="form-panel">
            <h3>{t("Shooting")}</h3>
            <p>{t("Use the recipe-aware flow so ammunition and component history remain traceable.")}</p>
            <Link className="button" to={`/sessions/shooting?firearmId=${encodeURIComponent(firearm.id)}`}>{t("Record session")}</Link>
          </div>
          <maintenanceFetcher.Form method="post"><input type="hidden" name="intent" value="maintenance" /><input type="hidden" name="idempotencyKey" value={maintenanceKey} /><label>{t("Event type")}<select name="eventType" defaultValue="General"><option value="General">{t("General")}</option><option value="Malfunction">{t("Malfunction")}</option><option value="Cleaning">{t("Cleaning")}</option><option value="Inspection">{t("Inspection")}</option></select></label><label>{t("Effective date and time")}<input name="date" type="datetime-local" defaultValue={localDateTimeInputValue()} required /></label><label>{t("Description")}<input name="description" required /></label><MaintenanceFeedback result={maintenanceResult} submitting={maintenanceFetcher.state !== "idle"} /><button className="button" type="submit" disabled={maintenanceFetcher.state !== "idle"}>{maintenanceFetcher.state === "submitting" ? t("Saving…") : t("Save event")}</button></maintenanceFetcher.Form>
        </div>
      </section>}
      <section className="panel">
        <div className="section-heading"><h2>{t("Maintenance history")}</h2><Link className="button button-quiet" to="/sessions/shooting?tab=maintenance">{t("Record maintenance")}</Link></div>
        {detail.maintenance.items.length === 0 ? <p>{t("No maintenance recorded.")}</p> : (
          <ul className="timeline">{detail.maintenance.items.map((event) => <li key={event.id} className={event.eventType === "Malfunction" ? "timeline-alert" : ""}><strong>{t(event.eventType)}</strong><time>{formatDate(event.date, culture)}</time><p>{event.description}</p>{event.notes && <small>{event.notes}</small>}</li>)}</ul>
        )}
        <HistoryPager kind="maintenance" page={detail.maintenance.page} hasMore={detail.maintenance.hasMore} searchParams={searchParams} />
      </section>
      <section className="panel firearm-history">
        <div className="section-heading"><h2>{t("Activity history")}</h2><Link className="button button-quiet" to={`/sessions/shooting?firearmId=${encodeURIComponent(firearm.id)}`}>{t("Record session")}</Link></div>
        <section className="round-history-section">
          <div className="section-heading">
            <h3>{t("Round history")}</h3>
            {points.length > 0 && (
              <div className="round-history-toggle" role="group" aria-label={t("Round history view")}>
                <button type="button" className={historyView === "chart" ? "button" : "button button-quiet"} aria-pressed={historyView === "chart"} onClick={() => setHistoryView("chart")}>{t("Chart")}</button>
                <button type="button" className={historyView === "table" ? "button" : "button button-quiet"} aria-pressed={historyView === "table"} onClick={() => setHistoryView("table")}>{t("Table")}</button>
              </div>
            )}
          </div>
          {points.length === 0 ? <p>{t("No round history recorded.")}</p> : (
            <>
              <p className="round-history-note">{t("Round history rows show recorded changes, including corrections and initial totals; session notes appear below.")}</p>
              <div className="round-history-layout" data-view={historyView}>
                <RoundHistoryChart points={points} maxRounds={maxRounds} culture={culture} />
                <RoundHistoryTable points={points} culture={culture} />
              </div>
            </>
          )}
        </section>
      </section>
    </div>
  );
}

export function MaintenanceFeedback({ result, submitting }: {
  result: { ok: boolean; error?: string; offline?: boolean } | null;
  submitting: boolean;
}) {
  const { t } = useI18n();
  if (!result || submitting) return null;
  return result.ok
    ? <p className="form-success" role="status">{t(result.offline ? "Maintenance queued for sync." : "Maintenance recorded.")}</p>
    : <p className="form-error" role="alert">{result.error}</p>;
}

function HistoryPager({ kind, page, hasMore, searchParams }: { kind: "installation" | "maintenance" | "shooting"; page: number; hasMore: boolean; searchParams: URLSearchParams }) {
  const { t } = useI18n();
  const makeUrl = (nextPage: number) => {
    const params = new URLSearchParams(searchParams);
    params.set(`${kind}Page`, String(nextPage));
    return `?${params.toString()}`;
  };
  return (page > 1 || hasMore) ? (
    <nav className="record-actions" aria-label={t(kind === "installation" ? "Installation history pages" : `${kind} history pages`)}>
      {page > 1 && <Link className="button button-quiet" to={makeUrl(page - 1)}>{t("Previous")}</Link>}
      <span>{t("Page {value}", { value: page })}</span>
      {hasMore && <Link className="button button-quiet" to={makeUrl(page + 1)}>{t("Next")}</Link>}
    </nav>
  ) : null;
}

function RoundHistoryChart({ points, maxRounds, culture }: {
  points: FirearmDetailLoaderData["roundHistory"];
  maxRounds: number;
  culture: ReturnType<typeof useI18n>["culture"];
}) {
  const { t } = useI18n();
  const left = 64;
  const right = 624;
  const top = 14;
  const bottom = 148;
  const start = new Date(points[0].effectiveAt).getTime();
  const end = new Date(points[points.length - 1].effectiveAt).getTime();
  const x = (index: number) => {
    if (end === start) return (left + right) / 2;
    const time = new Date(points[index].effectiveAt).getTime();
    return left + ((time - start) / (end - start)) * (right - left);
  };
  const y = (value: number) => bottom - (value / maxRounds) * (bottom - top);
  const ticks = [...new Set([maxRounds, Math.round(maxRounds / 2), 0])];
  const line = points.map((point, index) => `${x(index)},${y(point.cumulativeRounds)}`).join(" ");
  const startLabel = formatDate(points[0].effectiveAt, culture);
  const endLabel = formatDate(points[points.length - 1].effectiveAt, culture);
  return (
    <figure className="round-history-chart">
      <svg viewBox="0 0 640 184" role="img" aria-label={t("Cumulative round history from {start} to {end}", { start: startLabel, end: endLabel })}>
        <title>{t("Cumulative round history")}</title>
        <desc>{t("The vertical axis shows cumulative rounds; the horizontal axis shows activity dates.")}</desc>
        {ticks.map((tick) => (
          <g key={tick}>
            <line x1={left} x2={right} y1={y(tick)} y2={y(tick)} className="round-history-gridline" />
            <text x={left - 8} y={y(tick) + 4} textAnchor="end" className="round-history-axis-label">{formatNumber(tick, culture)}</text>
          </g>
        ))}
        <polyline fill="none" stroke="currentColor" strokeWidth="3" points={line} />
        {points.map((point, index) => <circle key={point.operationId} cx={x(index)} cy={y(point.cumulativeRounds)} r="3" fill="currentColor" />)}
        <text x={left} y="176" textAnchor="start" className="round-history-axis-label">{startLabel}</text>
        <text x={right} y="176" textAnchor="end" className="round-history-axis-label">{endLabel}</text>
      </svg>
    </figure>
  );
}

function RoundHistoryTable({ points, culture }: {
  points: FirearmDetailLoaderData["roundHistory"];
  culture: ReturnType<typeof useI18n>["culture"];
}) {
  const { t } = useI18n();
  return (
    <div className="round-history-table-wrap">
      <table className="round-history-table">
        <caption className="visually-hidden">{t("Round history details")}</caption>
        <thead><tr><th>{t("Date")}</th><th>{t("Change")}</th><th>{t("Cumulative")}</th></tr></thead>
        <tbody>{points.map((point) => (
          <tr key={point.operationId}>
            <td>{formatDate(point.effectiveAt, culture)}</td>
            <td>{formatNumber(point.delta, culture)}</td>
            <td>{formatNumber(point.cumulativeRounds, culture)}</td>
          </tr>
        ))}</tbody>
      </table>
    </div>
  );
}

function ComponentRow({ item, maxRounds }: { item: WorkshopComponentCondition; maxRounds: number }) {
  const { culture, t } = useI18n();
  const interval = item.component.trackingMode === "Interval" ? item.component.serviceInterval : null;
  const percentage = interval && interval > 0 ? item.component.totalRounds / interval * 100 : null;
  const state = percentage === null ? null : percentage >= 100 ? "Overdue" : percentage >= 90 ? "Attention" : percentage >= 70 ? "Due soon" : "Within interval";
  const relativePercentage = Math.round(item.component.totalRounds / maxRounds * 100);
  return <article className="component-usage-row">
    <div className="component-usage-heading">
      <div>
        <strong>{item.definition.name}</strong>
        <span>{t("Installed")}: <time dateTime={item.installation.installedAt}>{formatDate(item.installation.installedAt, culture)}</time></span>
        {item.component.identificationNote && <span>{item.component.identificationNote}</span>}
      </div>
      <strong className="component-usage-total">{formatNumber(item.component.totalRounds, culture)} {t("rounds")}</strong>
    </div>
    <div className="component-usage-meters">
      <div className="component-usage-meter-group">
        <div className="component-usage-meter-label"><span>{t("Relative use (% of highest-use component)")}</span><strong>{relativePercentage}%</strong></div>
        <div className="component-usage-meter component-usage-meter-relative" role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={relativePercentage} aria-valuetext={t("{percentage}% of highest-use component", { percentage: relativePercentage })} aria-label={t("Relative use compared with the highest-use component")}>
          <span style={{ width: `${relativePercentage}%` }} />
        </div>
      </div>
      {percentage !== null && state && (
        <div className="component-usage-meter-group">
          <div className="component-usage-meter-label">
            <span>{t("Service interval")}</span>
            <strong className={`component-interval-status interval-${state.toLowerCase().replace(" ", "-")}`}>{t(state)}</strong>
          </div>
          <small>{formatNumber(item.component.totalRounds, culture)} / {formatNumber(interval ?? 0, culture)} {t("rounds")}</small>
          <div className={`component-usage-meter component-usage-meter-interval interval-${state.toLowerCase().replace(" ", "-")}`} role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.min(percentage, 100)} aria-valuetext={t("{usage} of {interval} rounds: {status}", { usage: formatNumber(item.component.totalRounds, culture), interval: formatNumber(interval ?? 0, culture), status: t(state) })} aria-label={t("Service interval usage")}>
            <span style={{ width: `${Math.min(percentage, 100)}%` }} />
          </div>
        </div>
      )}
    </div>
  </article>;
}
