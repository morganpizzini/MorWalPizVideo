import {
  useState,
  type FormEvent
} from "react";
import {
  useNavigate,
  useRevalidator,
  useSearchParams
} from "react-router-dom";


import {
  freezerApi,
  rememberedAccountId,
  type AccountSession
} from "../freezerApi";
import {
  clearAccountOfflineData
} from "../offlineStore";

import {
  ErrorMessage,
  errorMessage,
  safeReturnTo
} from "./shared";

export function LoginPage() {
  const navigate = useNavigate();
  const revalidator = useRevalidator();
  const [searchParams] = useSearchParams();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setBusy(true);
    const form = new FormData(event.currentTarget);
    try {
      const previousAccountId = rememberedAccountId();
      let session: AccountSession;
      if (mode === "login") {
        session = await freezerApi.login(
          String(form.get("identifier") ?? ""),
          String(form.get("password") ?? ""),
        );
      } else {
        session = await freezerApi.register({
          username: String(form.get("username") ?? ""),
          email: String(form.get("email") ?? ""),
          displayName: String(form.get("displayName") ?? ""),
          password: String(form.get("password") ?? ""),
          termsAccepted: form.get("termsAccepted") === "on",
          privacyNoticeAcknowledged:
            form.get("privacyNoticeAcknowledged") === "on",
        });
      }
      if (
        previousAccountId &&
        session.account &&
        previousAccountId !== session.account.id
      )
        await clearAccountOfflineData(previousAccountId);
      revalidator.revalidate();
      navigate(safeReturnTo(searchParams.get("returnTo")));
    } catch (cause) {
      setError(errorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="auth-card">
      <h1>{mode === "login" ? "Bentornato." : "Crea il tuo account."}</h1>
      <div
        className="segmented-control"
        role="group"
        aria-label="Tipo di accesso"
      >
        <button
          className={mode === "login" ? "selected" : ""}
          onClick={() => setMode("login")}
        >
          Accedi
        </button>
        <button
          className={mode === "register" ? "selected" : ""}
          onClick={() => setMode("register")}
        >
          Registrati
        </button>
      </div>
      <form className="form-stack" onSubmit={(event) => void submit(event)}>
        {mode === "register" && (
          <>
            <label>
              Nome utente
              <input
                name="username"
                required
                minLength={3}
                maxLength={40}
                autoComplete="username"
              />
            </label>
            <label>
              Nome visualizzato
              <input name="displayName" maxLength={120} autoComplete="name" />
            </label>
            <label>
              Email
              <input name="email" type="email" required autoComplete="email" />
            </label>
          </>
        )}
        {mode === "login" && (
          <label>
            Email o nome utente
            <input name="identifier" required autoComplete="username" />
          </label>
        )}
        <label>
          Password
          <input
            name="password"
            type="password"
            required
            minLength={8}
            autoComplete={
              mode === "login" ? "current-password" : "new-password"
            }
          />
        </label>
        {mode === "register" && (
          <div className="checkbox-stack">
            <label className="checkbox-label">
              <input type="checkbox" name="termsAccepted" required /> Accetto i
              termini del servizio.
            </label>
            <label className="checkbox-label">
              <input
                type="checkbox"
                name="privacyNoticeAcknowledged"
                required
              />{" "}
              Ho letto l'informativa privacy.
            </label>
          </div>
        )}
        {error && <ErrorMessage>{error}</ErrorMessage>}
        <button className="button button-primary button-wide" disabled={busy}>
          {busy ? "Attendi…" : mode === "login" ? "Accedi" : "Crea account"}
        </button>
      </form>
    </section>
  );
}

