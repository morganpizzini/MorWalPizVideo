import type { ReactNode } from "react";
import { useI18n } from "../../i18n";

export function LoadingStatus({ label = "Loading data…" }: { label?: string }) {
  const { t } = useI18n();
  return (
    <span className="loading-status" role="status" aria-live="polite">
      <span className="loading-spinner" aria-hidden="true" />
      {t(label)}
    </span>
  );
}

export function SkeletonBlock({ className = "" }: { className?: string }) {
  return <span className={`skeleton-block ${className}`.trim()} aria-hidden="true" />;
}

export function CardGridSkeleton({ count = 4 }: { count?: number }) {
  const { t } = useI18n();
  return (
    <div className="card-grid loading-region" aria-busy="true">
      <span className="visually-hidden"><LoadingStatus label="Loading data…" /></span>
      {Array.from({ length: count }, (_, index) => (
        <div className="record-card skeleton-card" key={index} aria-hidden="true">
          <div className="record-card-top"><SkeletonBlock className="skeleton-eyebrow" /><SkeletonBlock className="skeleton-badge" /></div>
          <SkeletonBlock className="skeleton-title" />
          <SkeletonBlock className="skeleton-copy" />
          <SkeletonBlock className="skeleton-copy skeleton-copy-short" />
          <strong>{t("Loading")}</strong>
        </div>
      ))}
    </div>
  );
}

export function TableLoadingRow({ colSpan }: { colSpan: number }) {
  return (
    <tr className="table-loading-row">
      <td colSpan={colSpan}><LoadingStatus /></td>
    </tr>
  );
}

export function ActivitySkeleton({ rows = 5, compact = false }: { rows?: number; compact?: boolean }) {
  return (
    <div className={`activity-skeleton${compact ? " activity-skeleton-compact" : ""} loading-region`} aria-busy="true">
      <span className="visually-hidden"><LoadingStatus label="Loading activity…" /></span>
      {Array.from({ length: rows }, (_, index) => (
        <div className="activity-skeleton-row" key={index} aria-hidden="true">
          <SkeletonBlock className="skeleton-dot" />
          <div><SkeletonBlock className="skeleton-copy" /><SkeletonBlock className="skeleton-copy skeleton-copy-short" /></div>
          <SkeletonBlock className="skeleton-date" />
        </div>
      ))}
    </div>
  );
}

export function MetricSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="stats-grid loading-region" aria-busy="true">
      <span className="visually-hidden"><LoadingStatus label="Loading summary…" /></span>
      {Array.from({ length: count }, (_, index) => (
        <div className="stat skeleton-stat" key={index} aria-hidden="true">
          <SkeletonBlock className="skeleton-copy" />
          <SkeletonBlock className="skeleton-value" />
        </div>
      ))}
    </div>
  );
}

export function PanelSkeleton({ children, className = "" }: { children?: ReactNode; className?: string }) {
  return (
    <div className={`panel loading-region ${className}`.trim()} aria-busy="true">
      <span className="visually-hidden"><LoadingStatus /></span>
      {children ?? <><SkeletonBlock className="skeleton-heading" /><SkeletonBlock className="skeleton-copy skeleton-copy-long" /><SkeletonBlock className="skeleton-copy skeleton-copy-medium" /></>}
    </div>
  );
}

export function DetailSkeleton() {
  return (
    <div className="loading-region" aria-busy="true">
      <span className="visually-hidden"><LoadingStatus label="Loading details…" /></span>
      <SkeletonBlock className="skeleton-detail-title" />
      <MetricSkeleton count={4} />
      <PanelSkeleton className="skeleton-detail-panel" />
      <PanelSkeleton className="skeleton-detail-panel" />
    </div>
  );
}
