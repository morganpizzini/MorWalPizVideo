import { Suspense, type ReactNode } from "react";
import { Await, useAsyncError } from "react-router";

export function RangeSkeleton({
  rows = 3,
  label = "Caricamento dati...",
}: {
  rows?: number;
  label?: string;
}) {
  return (
    <div className="range-skeleton" aria-busy="true">
      <span className="sr-only" role="status" aria-live="polite">
        {label}
      </span>
      {Array.from({ length: rows }, (_, index) => (
        <div className="range-skeleton__row" key={index} aria-hidden="true">
          <span />
          <span />
          <span />
        </div>
      ))}
    </div>
  );
}

export function RangeDeferred<T>({
  resolve,
  fallback = <RangeSkeleton />,
  onRetry,
  children,
}: {
  resolve: Promise<T>;
  fallback?: ReactNode;
  onRetry?: () => void;
  children: (value: T) => ReactNode;
}) {
  return (
    <Suspense fallback={fallback}>
      <Await
        resolve={resolve}
        errorElement={<DeferredLoadError onRetry={onRetry} />}
      >
        {children}
      </Await>
    </Suspense>
  );
}

function DeferredLoadError({ onRetry }: { onRetry?: () => void }) {
  const reason = useAsyncError();
  const message =
    reason instanceof Error ? reason.message : "I dati non sono disponibili.";

  return (
    <div className="range-load-error" role="alert">
      <p>{message}</p>
      {onRetry && (
        <button
          type="button"
          className="button button-quiet"
          onClick={onRetry}
        >
          Riprova
        </button>
      )}
    </div>
  );
}

export function RouteSessionError({
  message,
  status,
  onRetry,
}: {
  message: string;
  status?: number;
  onRetry?: () => void;
}) {
  return (
    <section className="range-panel" role="alert">
      <h2>{status === 403 ? "Accesso non autorizzato" : "Sessione non disponibile"}</h2>
      <p>{message}</p>
      {onRetry && (
        <button type="button" className="button button-quiet" onClick={onRetry}>
          Riprova
        </button>
      )}
    </section>
  );
}
