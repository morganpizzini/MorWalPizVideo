import { Link } from "react-router";
import type { ReactNode } from "react";
import { useI18n } from "../i18n";

export function LoadingState({ label }: { label?: string }) {
  const { t } = useI18n();
  return <p className="state state-loading" role="status">{label ?? t("Loading workshop data…")}</p>;
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useI18n();
  const message = error instanceof Error ? error.message : t("The request could not be completed.");
  return (
    <div className="state state-error" role="alert">
      <strong>{t("Could not load this workspace.")}</strong>
      <p>{message}</p>
      {onRetry && <button className="button button-quiet" onClick={onRetry}>{t("Try again")}</button>}
    </div>
  );
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="state state-empty">
      <strong>{title}</strong>
      {children && <p>{children}</p>}
    </div>
  );
}

export function ConflictState({ message = "Another operation changed this record. Reload before saving again." }: { message?: string }) {
  const { t } = useI18n();
  return <div className="state state-conflict" role="alert"><strong>{t("Conflict detected")}</strong><p>{message === "Another operation changed this record. Reload before saving again." ? t(message) : message}</p><Link className="button button-quiet" to="">{t("Reload")}</Link></div>;
}
