import { Link } from "react-router";
import type { ReactNode } from "react";

export function LoadingState({ label = "Loading workshop data…" }: { label?: string }) {
  return <p className="state state-loading" role="status">{label}</p>;
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof Error ? error.message : "The request could not be completed.";
  return (
    <div className="state state-error" role="alert">
      <strong>Could not load this workspace.</strong>
      <p>{message}</p>
      {onRetry && <button className="button button-quiet" onClick={onRetry}>Try again</button>}
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
  return <div className="state state-conflict" role="alert"><strong>Conflict detected</strong><p>{message}</p><Link className="button button-quiet" to="">Reload</Link></div>;
}
