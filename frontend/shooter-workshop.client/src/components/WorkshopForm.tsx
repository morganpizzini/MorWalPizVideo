import { useState, type FormEvent, type ReactNode } from "react";
import { getApiFieldErrors } from "@morwalpizvideo/services";

export function WorkshopForm({
  title,
  submitLabel,
  children,
  onSubmit,
}: {
  title: string;
  submitLabel: string;
  children: ReactNode;
  onSubmit: () => Promise<void>;
}) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      await onSubmit();
    } catch (requestError) {
      const fields = getApiFieldErrors(requestError);
      setError(Object.values(fields).flat().join(" ") || (requestError instanceof Error ? requestError.message : "Please review the form."));
    } finally {
      setPending(false);
    }
  };
  return (
    <form className="panel form-panel" onSubmit={submit}>
      <h2>{title}</h2>
      {children}
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="button button-primary" disabled={pending}>{pending ? "Saving…" : submitLabel}</button>
    </form>
  );
}
