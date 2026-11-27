import { useEffect, useState, type ReactNode } from "react";
import type { PublicAccountTokenRequest } from "@morwalpizvideo/models";
import { AccountForm } from "./AccountForm.js";

export type AccountMailTranslate = (message: string) => string;
export const accountMailItalian: Record<string, string> = {
  "Recover your password": "Recupera la password",
  "Choose a new password": "Scegli una nuova password",
  "Check your email": "Controlla la tua email",
  "Confirm your email": "Conferma la tua email",
  "Email": "Email",
  "New password": "Nuova password",
  "Confirm password": "Conferma password",
  "Passwords do not match.": "Le password non coincidono.",
  "Sign in": "Accedi",
  "Back to sign in": "Torna all'accesso",
  "Forgot your password?": "Password dimenticata?",
  "Request a new email": "Richiedi una nuova email",
  "Request again in": "Puoi richiedere un'altra email tra",
  "Send email": "Invia email",
  "Save password": "Salva password",
  "Confirm email": "Conferma email",
  "Submitting…": "Invio in corso…",
  "Password updated. Sign in with your new password.": "Password aggiornata. Accedi con la nuova password.",
  "Email confirmed. You can now sign in.": "Email confermata. Ora puoi accedere.",
  "This link is missing or invalid. Request a new email.": "Il link manca o non è valido. Richiedi una nuova email.",
  "Confirmation requested. Open the email link and confirm before signing in.": "Conferma richiesta. Apri il link ricevuto e conferma l'email prima di accedere.",
  "If the account is eligible, you will receive an email. Check your inbox and spam folder.": "Se l'account è idoneo, riceverai un'email. Controlla la posta in arrivo e la cartella spam.",
  "Confirm your email before signing in. Request a confirmation email below.": "Conferma l'email prima di accedere. Richiedi un'email di conferma qui sotto.",
  "Confirm that this email address belongs to you. Opening the link alone does not confirm it.": "Conferma che questo indirizzo email ti appartiene. Aprire il link non basta a confermarlo.",
  "Email confirmation": "Conferma email",
  "Confirmation requested. Check your inbox and confirm your email.": "Conferma richiesta. Controlla la posta e conferma la tua email.",
  "Please confirm your email address. You can continue using your existing account.": "Conferma il tuo indirizzo email. Puoi continuare a usare il tuo account esistente.",
  "Request a confirmation email": "Richiedi un'email di conferma",
  "I have confirmed my email": "Ho confermato la mia email",
  "Reconnect to request or check email confirmation.": "Riconnettiti per richiedere o verificare la conferma dell'email.",
  "The request or link is invalid or expired. Check the details or request a new email.": "La richiesta o il link non è valido o è scaduto. Controlla i dati o richiedi una nuova email.",
  "The security request expired. Reload the page and try again.": "La richiesta di sicurezza è scaduta. Ricarica la pagina e riprova.",
  "Too many requests. Wait a few minutes and try again.": "Troppe richieste. Attendi qualche minuto e riprova.",
  "Account mail is unavailable. Check your connection and try again.": "Il servizio email non è disponibile. Controlla la connessione e riprova.",
};

export function useAccountMailCooldown(initiallyRequested = false) {
  const [deadline, setDeadline] = useState(() => initiallyRequested ? Date.now() + 60_000 : 0);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    if (!deadline) return;
    const tick = () => setNow(Date.now());
    const timer = window.setInterval(tick, 250);
    window.addEventListener("focus", tick);
    return () => { window.clearInterval(timer); window.removeEventListener("focus", tick); };
  }, [deadline]);
  return {
    remaining: Math.max(0, Math.ceil((deadline - now) / 1000)),
    start: () => { const time = Date.now(); setNow(time); setDeadline(time + 60_000); },
  };
}

interface AccountMailPageProps {
  mode: "request-reset" | "reset" | "check-email" | "confirm";
  t: AccountMailTranslate;
  onRequest: (email: string) => Promise<void>;
  onConsume: (link: PublicAccountTokenRequest, password: string) => Promise<void>;
  errorMessage: (error: unknown) => string;
  link?: PublicAccountTokenRequest | null;
  initialEmail?: string;
  initiallyRequested?: boolean;
  loginPath?: string;
  requestPath?: string;
  header?: ReactNode;
  submitClassName?: string;
}

export function AccountMailPage({ mode, t, onRequest, onConsume, errorMessage, link, initialEmail = "",
  initiallyRequested = false, loginPath = "/account/login", requestPath = "/account/forgot-password",
  header, submitClassName }: AccountMailPageProps) {
  const [email, setEmail] = useState(initialEmail);
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [requested, setRequested] = useState(initiallyRequested);
  const [completed, setCompleted] = useState(false);
  const { remaining, start } = useAccountMailCooldown(initiallyRequested);
  const requestMode = mode === "request-reset" || mode === "check-email";
  const title = mode === "request-reset" ? "Recover your password" : mode === "reset"
    ? "Choose a new password" : mode === "check-email" ? "Check your email" : "Confirm your email";
  const submit = async () => {
    if (pending || (requestMode && remaining)) return;
    setError("");
    if (mode === "reset" && password !== confirmation) { setError(t("Passwords do not match.")); return; }
    setPending(true);
    try {
      if (requestMode) { await onRequest(email); setRequested(true); start(); }
      else if (link) { await onConsume(link, password); setCompleted(true); setPassword(""); setConfirmation(""); }
    } catch (requestError) {
      setError(errorMessage(requestError));
    } finally { setPending(false); }
  };
  if (completed || (!requestMode && !link)) {
    return <main className="account-page"><section className="account-card card mx-auto"><div className="card-body">
      {header}<h1>{t(title)}</h1>
      <p role="status">{t(completed
        ? mode === "reset" ? "Password updated. Sign in with your new password." : "Email confirmed. You can now sign in."
        : "This link is missing or invalid. Request a new email.")}</p>
      <a href={completed ? loginPath : requestPath}>{t(completed ? "Sign in" : "Request a new email")}</a>
    </div></section></main>;
  }
  return <AccountForm title={t(title)} submitLabel={remaining > 0 && requestMode
    ? `${t("Request again in")} ${remaining}s` : t(requestMode ? requested ? "Request a new email" : "Send email" : mode === "reset" ? "Save password" : "Confirm email")}
    pendingLabel={t("Submitting…")} submitClassName={submitClassName} header={header}
    pending={pending} submitDisabled={requestMode && remaining > 0} error={error}
    message={requested ? t(mode === "check-email" ? "Confirmation requested. Open the email link and confirm before signing in."
      : "If the account is eligible, you will receive an email. Check your inbox and spam folder.") : undefined}
    onSubmit={(event) => { event.preventDefault(); void submit(); }}
    footer={<p className="mt-3"><a href={loginPath}>{t("Back to sign in")}</a></p>}>
    {requestMode ? <label className="form-label d-block">{t("Email")}
      <input className="form-control" type="email" required maxLength={256} autoComplete="email"
        value={email} onChange={(event) => setEmail(event.target.value)} disabled={pending} />
    </label> : mode === "reset" ? <>
      <label className="form-label d-block">{t("New password")}<input className="form-control" type="password" required
        minLength={8} maxLength={128} autoComplete="new-password" value={password}
        onChange={(event) => setPassword(event.target.value)} disabled={pending} /></label>
      <label className="form-label d-block mt-3">{t("Confirm password")}<input className="form-control" type="password" required
        minLength={8} maxLength={128} autoComplete="new-password" value={confirmation}
        onChange={(event) => setConfirmation(event.target.value)} disabled={pending} /></label>
    </> : <p>{t("Confirm that this email address belongs to you. Opening the link alone does not confirm it.")}</p>}
    {mode === "check-email" && !requested && <p role="status">{t("Confirm your email before signing in. Request a confirmation email below.")}</p>}
  </AccountForm>;
}

interface AccountEmailNoticeProps {
  t: AccountMailTranslate;
  onRequest: () => Promise<void>;
  errorMessage: (error: unknown) => string;
  onRefresh: () => Promise<unknown>;
  disabled?: boolean;
  initiallyRequested?: boolean;
}

export function AccountEmailNotice({ t, onRequest, errorMessage, onRefresh, disabled = false, initiallyRequested = false }: AccountEmailNoticeProps) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [requested, setRequested] = useState(initiallyRequested);
  const { remaining, start } = useAccountMailCooldown(initiallyRequested);
  const send = async () => {
    setPending(true); setError("");
    try { await onRequest(); setRequested(true); start(); }
    catch (requestError) { setError(errorMessage(requestError)); }
    finally { setPending(false); }
  };
  return <section className="alert alert-warning" aria-label={t("Email confirmation")}>
    <p role="status">{t(requested ? "Confirmation requested. Check your inbox and confirm your email."
      : "Please confirm your email address. You can continue using your existing account.")}</p>
    {error && <p role="alert">{error}</p>}
    <button type="button" className="btn btn-outline-secondary" disabled={disabled || pending || remaining > 0}
      onClick={() => void send()}>{pending ? t("Submitting…") : remaining
        ? `${t("Request again in")} ${remaining}s` : t("Request a confirmation email")}</button>{" "}
    <button type="button" className="btn btn-outline-secondary" disabled={disabled || pending}
      onClick={async () => {
        setPending(true); setError("");
        try { await onRefresh(); }
        catch (refreshError) { setError(errorMessage(refreshError)); }
        finally { setPending(false); }
      }}>{t("I have confirmed my email")}</button>
    {disabled && <p>{t("Reconnect to request or check email confirmation.")}</p>}
  </section>;
}
