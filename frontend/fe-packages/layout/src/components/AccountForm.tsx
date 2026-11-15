import type { FormEvent, ReactNode } from "react";

export interface AccountFormProps {
  title: string;
  submitLabel: string;
  pendingLabel?: string;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  error?: string;
  message?: string;
  warning?: string;
  pending?: boolean;
  header?: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
}

export function AccountForm({
  title,
  submitLabel,
  pendingLabel = "Submitting…",
  onSubmit,
  error,
  message,
  warning,
  pending = false,
  header,
  footer,
  children,
}: AccountFormProps) {
  return (
    <main className="account-page">
      <section className="account-card card card-no-float mx-auto">
        <div className="card-body">
          {header}
          <h1>{title}</h1>
          <form onSubmit={onSubmit}>
            {children}
            {error && (
              <div className="alert alert-danger mt-3" role="alert">
                {error}
              </div>
            )}
            {message && (
              <div className="alert alert-success mt-3" role="status">
                {message}
              </div>
            )}
            {warning && (
              <div className="alert alert-warning mt-3" role="status">
                {warning}
              </div>
            )}
            <button type="submit" className="btn btn-primary mt-4" disabled={pending}>
              {pending ? pendingLabel : submitLabel}
            </button>
            {footer}
          </form>
        </div>
      </section>
    </main>
  );
}
