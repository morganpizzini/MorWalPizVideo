import { Link, useFetcher, useLoaderData, useSearchParams } from "react-router";
import { useEffect, useRef, useState } from "react";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { WorkshopComponentCondition, WorkshopFirearmDetail } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { formatDate, formatNumber, localDateTimeInputValue } from "../../i18n-formatters";
import { Stat } from "../shared";
import type { action } from "./action";

export default function FirearmDetail() {
  const { culture, t } = useI18n();
  const detail = useLoaderData() as WorkshopFirearmDetail;
  const [searchParams] = useSearchParams();
  const maintenanceFetcher = useFetcher<typeof action>();
  const [maintenanceKey, setMaintenanceKey] = useState(() => crypto.randomUUID());
  const processedMaintenance = useRef<unknown>(null);
  useEffect(() => {
    if (maintenanceFetcher.state !== "idle" || !maintenanceFetcher.data || !("ok" in maintenanceFetcher.data) ||
        !maintenanceFetcher.data.ok || maintenanceFetcher.data === processedMaintenance.current) return;
    processedMaintenance.current = maintenanceFetcher.data;
    setMaintenanceKey(crypto.randomUUID());
  }, [maintenanceFetcher.state, maintenanceFetcher.data]);
  const maintenanceResult = maintenanceFetcher.data && "ok" in maintenanceFetcher.data ? maintenanceFetcher.data : null;
  if (!detail?.firearm) {
    return <EmptyState title={t("Firearm not found")} message={t("The record may be archived or belong to another account.")} />;
  }
  const firearm = detail.firearm;
  const title = firearm.nickname || `${firearm.manufacturer} ${firearm.model}`;
  const points = detail.roundHistory;
  const maxRounds = Math.max(...points.map((point) => point.cumulativeRounds), firearm.totalRounds, 1);
  return (
    <div className="firearm-detail">
      <Link className="back-link" to="/firearms">← {t("Firearms")}</Link>
      <PageTitle title={title} subtitle={`${firearm.caliber}${firearm.category ? ` · ${firearm.category}` : ""}`} />
      <div className="stats-grid">
        <Stat label="Total rounds" value={formatNumber(firearm.totalRounds, culture)} />
        <Stat label="Status" value={t(firearm.archived ? "archived" : "active")} />
        <Stat label="Malfunctions" value={detail.malfunctionCount} />
        <Stat label="Installed components" value={detail.components.length} />
      </div>
      <section className="panel firearm-round-history">
        <div className="section-heading"><h2>{t("Round history")}</h2><strong>{formatNumber(firearm.totalRounds, culture)} {t("rounds")}</strong></div>
        {points.length === 0 ? <p>{t("No round history recorded.")}</p> : (
          <svg className="round-chart" viewBox="0 0 640 160" role="img" aria-label={t("Cumulative round history")}>
            <polyline fill="none" stroke="currentColor" strokeWidth="3"
              points={points.map((point, index) => `${(index / Math.max(points.length - 1, 1)) * 620 + 10},${150 - (point.cumulativeRounds / maxRounds) * 130}`).join(" ")} />
          </svg>
        )}
        {detail.projectionState !== "Current" && <p className="status-note">{t("Projection state")}: {t(detail.projectionState)}</p>}
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Installed components")}</h2><Link className="button button-quiet" to="/components">{t("Manage components")}</Link></div>
        {detail.components.length === 0 ? <p>{t("No components installed.")}</p> : (
          <div className="detail-list">{detail.components.map((item) => <ComponentRow key={item.installation.id} item={item} />)}</div>
        )}
        <Link className="button button-quiet" to={`/components?firearmId=${encodeURIComponent(firearm.id)}`}>{t("Install, swap, or detach a component")}</Link>
      </section>
      {!firearm.archived && <section className="panel firearm-actions">
        <div className="section-heading"><h2>{t("Record activity")}</h2><form method="post"><input type="hidden" name="intent" value="archive" /><input type="hidden" name="revision" value={firearm.revision} /><button className="button button-quiet" type="submit">{t("Archive firearm")}</button></form></div>
        <div className="form-grid">
          <div className="form-panel">
            <h3>{t("Shooting")}</h3>
            <p>{t("Use the recipe-aware flow so ammunition and component history remain traceable.")}</p>
            <Link className="button" to={`/sessions/shooting?firearmId=${encodeURIComponent(firearm.id)}`}>{t("Record session")}</Link>
          </div>
          <maintenanceFetcher.Form method="post"><input type="hidden" name="intent" value="maintenance" /><input type="hidden" name="idempotencyKey" value={maintenanceKey} /><label>{t("Event type")}<select name="eventType" defaultValue="General"><option value="General">{t("General")}</option><option value="Malfunction">{t("Malfunction")}</option><option value="Cleaning">{t("Cleaning")}</option><option value="Inspection">{t("Inspection")}</option></select></label><label>{t("Effective date and time")}<input name="date" type="datetime-local" defaultValue={localDateTimeInputValue()} required /></label><label>{t("Description")}<input name="description" required /></label>{maintenanceFetcher.state === "idle" && maintenanceResult && !maintenanceResult.ok && <p className="form-error" role="alert">{maintenanceResult.error}</p>}<button className="button" type="submit" disabled={maintenanceFetcher.state !== "idle"}>{maintenanceFetcher.state === "submitting" ? t("Saving…") : t("Save event")}</button></maintenanceFetcher.Form>
        </div>
      </section>}
      <section className="panel">
        <div className="section-heading"><h2>{t("Maintenance history")}</h2><Link className="button button-quiet" to="/maintenance">{t("Record maintenance")}</Link></div>
        {detail.maintenance.items.length === 0 ? <p>{t("No maintenance recorded.")}</p> : (
          <ul className="timeline">{detail.maintenance.items.map((event) => <li key={event.id} className={event.eventType === "Malfunction" ? "timeline-alert" : ""}><strong>{t(event.eventType)}</strong><time>{formatDate(event.date, culture)}</time><p>{event.description}</p>{event.notes && <small>{event.notes}</small>}</li>)}</ul>
        )}
        <HistoryPager kind="maintenance" page={detail.maintenance.page} hasMore={detail.maintenance.hasMore} searchParams={searchParams} />
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Shooting history")}</h2><Link className="button button-quiet" to={`/sessions/shooting?firearmId=${encodeURIComponent(firearm.id)}`}>{t("Record session")}</Link></div>
        {detail.shooting.items.length === 0 ? <p>{t("No shooting sessions recorded.")}</p> : (
          <ul className="timeline">{detail.shooting.items.map(({ session, superseded }) => <li key={session.id} className={superseded ? "timeline-muted" : ""}><strong>{formatNumber(session.totalRounds, culture)} {t("rounds")}{superseded && ` · ${t("superseded")}`}</strong><time>{formatDate(session.date, culture)}</time>{session.notes && <p>{session.notes}</p>}</li>)}</ul>
        )}
        <HistoryPager kind="shooting" page={detail.shooting.page} hasMore={detail.shooting.hasMore} searchParams={searchParams} />
      </section>
    </div>
  );
}

function HistoryPager({ kind, page, hasMore, searchParams }: { kind: "maintenance" | "shooting"; page: number; hasMore: boolean; searchParams: URLSearchParams }) {
  const { t } = useI18n();
  const makeUrl = (nextPage: number) => {
    const params = new URLSearchParams(searchParams);
    params.set(`${kind}Page`, String(nextPage));
    return `?${params.toString()}`;
  };
  return (page > 1 || hasMore) ? (
    <nav className="record-actions" aria-label={t(`${kind} history pages`)}>
      {page > 1 && <Link className="button button-quiet" to={makeUrl(page - 1)}>{t("Previous")}</Link>}
      <span>{t("Page {value}", { value: page })}</span>
      {hasMore && <Link className="button button-quiet" to={makeUrl(page + 1)}>{t("Next")}</Link>}
    </nav>
  ) : null;
}

function ComponentRow({ item }: { item: WorkshopComponentCondition }) {
  const { culture, t } = useI18n();
  const interval = item.component.trackingMode === "Interval" ? item.component.serviceInterval : null;
  const percentage = interval && interval > 0 ? item.component.totalRounds / interval * 100 : null;
  const state = percentage === null ? null : percentage >= 100 ? "overdue" : percentage >= 90 ? "attention" : percentage >= 70 ? "due soon" : "within interval";
  return <article className="detail-list-item">
    <div><strong>{item.definition.name}</strong><span>{item.component.identificationNote || t("No identification note")}</span></div>
    <span>{formatNumber(item.component.totalRounds, culture)} {t("rounds")}</span>
    {percentage !== null && <div className={`interval-meter interval-${state?.replace(" ", "-")}`} role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.min(percentage, 100)} aria-label={`${state}: ${percentage.toFixed(1)}%`}><span style={{ width: `${Math.min(percentage, 100)}%` }} /></div>}
  </article>;
}
