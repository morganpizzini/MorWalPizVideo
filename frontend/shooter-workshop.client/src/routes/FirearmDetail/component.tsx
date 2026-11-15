import { Form, Link, useLoaderData } from "react-router";
import { PageTitle, EmptyState } from "@morwalpiz/layout";
import type { WorkshopComponentCondition, WorkshopFirearmDetail } from "@morwalpizvideo/models";
import { useI18n } from "../../i18n";
import { formatDate, formatNumber } from "../../i18n-formatters";
import { Stat } from "../shared";

export default function FirearmDetail() {
  const { culture, t } = useI18n();
  const detail = useLoaderData() as WorkshopFirearmDetail;
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
      </section>
      {!firearm.archived && <section className="panel firearm-actions">
        <div className="section-heading"><h2>{t("Record activity")}</h2><form method="post"><input type="hidden" name="intent" value="archive" /><input type="hidden" name="revision" value={firearm.revision} /><button className="button button-quiet" type="submit">{t("Archive firearm")}</button></form></div>
        <div className="form-grid">
          <Form method="post"><input type="hidden" name="intent" value="shooting" /><label>{t("Rounds")}<input name="totalRounds" type="number" min="1" required /></label><label>{t("Notes")}<input name="notes" /></label><button className="button" type="submit">{t("Record session")}</button></Form>
          <Form method="post"><input type="hidden" name="intent" value="maintenance" /><label>{t("Event type")}<select name="eventType" defaultValue="General"><option value="General">{t("General")}</option><option value="Malfunction">{t("Malfunction")}</option><option value="Cleaning">{t("Cleaning")}</option><option value="Inspection">{t("Inspection")}</option></select></label><label>{t("Description")}<input name="description" required /></label><button className="button" type="submit">{t("Save event")}</button></Form>
        </div>
      </section>}
      <section className="panel">
        <div className="section-heading"><h2>{t("Maintenance history")}</h2><Link className="button button-quiet" to="/maintenance">{t("Record maintenance")}</Link></div>
        {detail.maintenance.items.length === 0 ? <p>{t("No maintenance recorded.")}</p> : (
          <ul className="timeline">{detail.maintenance.items.map((event) => <li key={event.id} className={event.eventType === "Malfunction" ? "timeline-alert" : ""}><strong>{t(event.eventType)}</strong><time>{formatDate(event.date, culture)}</time><p>{event.description}</p>{event.notes && <small>{event.notes}</small>}</li>)}</ul>
        )}
      </section>
      <section className="panel">
        <div className="section-heading"><h2>{t("Shooting history")}</h2><Link className="button button-quiet" to="/sessions/shooting">{t("Record session")}</Link></div>
        {detail.shooting.items.length === 0 ? <p>{t("No shooting sessions recorded.")}</p> : (
          <ul className="timeline">{detail.shooting.items.map(({ session }) => <li key={session.id}><strong>{formatNumber(session.totalRounds, culture)} {t("rounds")}</strong><time>{formatDate(session.date, culture)}</time>{session.notes && <p>{session.notes}</p>}</li>)}</ul>
        )}
      </section>
    </div>
  );
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
