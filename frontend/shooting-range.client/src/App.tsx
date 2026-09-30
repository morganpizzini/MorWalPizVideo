import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { Link, useNavigate } from "react-router";
import {
  ApiError,
  availability,
  book,
  changePassword,
  getSession,
  login,
  logout,
  myBookings,
  register,
  sessions,
  type Availability,
  type Account,
  type Booking as StoredBooking,
  type Sessions,
} from "./api";
import {
  accountStatus,
  availabilityMatches,
  bookingStatus,
  sessionLabel,
} from "./schedule";

export interface SessionViewProps {
  account: Account;
  onLogout: () => void;
  loggingOut: boolean;
  onPasswordChanged: () => void;
}

export function Shell({
  account,
  children,
  onLogout,
  loggingOut = false,
}: {
  account: Account | null;
  children: ReactNode;
  onLogout?: () => void;
  loggingOut?: boolean;
}) {
  return (
    <main>
      <header>
        <div>
          <span className="eyebrow">PEPPERBOX</span>
          <h1>Campo da tiro</h1>
        </div>
        <nav aria-label="Navigazione principale">
          <Link to="/">Prenota</Link>
          {!account?.forcePasswordChange && (
            <Link to="/messages">Messaggi</Link>
          )}
          {account?.isAdmin && !account.forcePasswordChange && (
            <Link to="/admin">Admin</Link>
          )}
          {account && onLogout && (
            <button
              className="secondary-button"
              disabled={loggingOut}
              onClick={onLogout}
            >
              {loggingOut ? "Uscita..." : "Esci"}
            </button>
          )}
        </nav>
      </header>
      {children}
    </main>
  );
}

export function Auth({ onLogin }: { onLogin: (account: Account) => void }) {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [form, setForm] = useState({
    username: "",
    password: "",
    firstName: "",
    lastName: "",
  });
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState("");
  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError("");
    setNotice("");
    try {
      const result =
        mode === "login" ? await login(form) : await register(form);
      if (mode === "login") onLogin(result);
      else {
        setMode("login");
        setNotice(
          "Richiesta inviata. Attendi l'approvazione dell'amministratore.",
        );
      }
    } catch (reason) {
      setError(
        reason instanceof ApiError && reason.status === 429
          ? "Troppi tentativi. Riprova tra cinque minuti."
          : reason instanceof Error
            ? reason.message
            : "Operazione non riuscita",
      );
    } finally {
      setPending(false);
    }
  }
  return (
    <section className="auth">
      <span className="eyebrow">ACCESSO</span>
      <h2>{mode === "login" ? "Bentornato" : "Richiedi un account"}</h2>
      <p className="muted">
        Gli account nuovi restano in attesa dell'approvazione
        dell'amministratore.
      </p>
      <form onSubmit={submit}>
        <fieldset disabled={pending} className="auth-fields">
          {mode === "register" && (
            <>
              <label>
                Nome
                <input
                  required
                  value={form.firstName}
                  autoComplete="given-name"
                  onChange={(event) =>
                    setForm({ ...form, firstName: event.target.value })
                  }
                />
              </label>
              <label>
                Cognome
                <input
                  required
                  value={form.lastName}
                  autoComplete="family-name"
                  onChange={(event) =>
                    setForm({ ...form, lastName: event.target.value })
                  }
                />
              </label>
            </>
          )}
          <label>
            Username
            <input
              required
              value={form.username}
              autoComplete="username"
              onChange={(event) =>
                setForm({ ...form, username: event.target.value })
              }
            />
          </label>
          <label>
            Password
            <input
              required
              minLength={12}
              type="password"
              value={form.password}
              autoComplete={
                mode === "login" ? "current-password" : "new-password"
              }
              onChange={(event) =>
                setForm({ ...form, password: event.target.value })
              }
            />
          </label>
          <button>
            {pending
              ? "Attendi..."
              : mode === "login"
                ? "Accedi"
                : "Invia richiesta"}
          </button>
        </fieldset>
      </form>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {notice && (
        <p role="status" className="success">
          {notice}
        </p>
      )}
      <button
        disabled={pending}
        className="link-button"
        onClick={() => {
          setMode(mode === "login" ? "register" : "login");
          setError("");
        }}
      >
        {mode === "login" ? "Non hai un account?" : "Hai già un account?"}
      </button>
    </section>
  );
}

export function SessionGate({
  children,
}: {
  children?: (props: SessionViewProps) => ReactNode;
}) {
  const [account, setAccount] = useState<Account | null>(null);
  const [restoring, setRestoring] = useState(true);
  const [loggingOut, setLoggingOut] = useState(false);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const navigate = useNavigate();
  useEffect(() => {
    const controller = new AbortController();
    setRestoring(true);
    setError("");
    getSession(controller.signal)
      .then((value) => {
        if (!controller.signal.aborted) setAccount(value);
      })
      .catch((reason) => {
        if (!controller.signal.aborted) {
          setAccount(null);
          if (!(reason instanceof ApiError && reason.status === 401))
            setError("Sessione non disponibile. Riprova.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setRestoring(false);
      });
    return () => controller.abort();
  }, [revision]);
  useEffect(() => {
    function expired() {
      setAccount(null);
      navigate("/");
    }
    function refresh() {
      setRevision((value) => value + 1);
    }
    window.addEventListener("range-session-expired", expired);
    window.addEventListener("focus", refresh);
    return () => {
      window.removeEventListener("range-session-expired", expired);
      window.removeEventListener("focus", refresh);
    };
  }, [navigate]);
  async function signOut() {
    if (loggingOut) return;
    setLoggingOut(true);
    setError("");
    try {
      await logout();
      setAccount(null);
      navigate("/");
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 401) {
        setAccount(null);
        navigate("/");
      } else setError("Uscita non riuscita. Riprova.");
    } finally {
      setLoggingOut(false);
    }
  }
  if (restoring)
    return (
      <Shell account={null}>
        <p role="status" className="session-status">
          Ripristino sessione...
        </p>
      </Shell>
    );
  if (!account)
    return (
      <Shell account={null}>
        {error && (
          <div role="alert" className="error">
            {error}
            <button
              className="secondary-button"
              onClick={() => setRevision((value) => value + 1)}
            >
              Riprova
            </button>
          </div>
        )}
        <Auth onLogin={setAccount} />
      </Shell>
    );
  const props: SessionViewProps = {
    account,
    loggingOut,
    onLogout: () => void signOut(),
    onPasswordChanged: () => setRevision((value) => value + 1),
  };
  return (
    <>
      {error && (
        <p role="alert" className="error session-error">
          {error}
        </p>
      )}
      {account.forcePasswordChange || !children ? (
        <Booking {...props} />
      ) : (
        children(props)
      )}
    </>
  );
}

export function Booking({
  account,
  onPasswordChanged,
  onLogout,
  loggingOut,
}: SessionViewProps) {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [period, setPeriod] = useState("");
  const [offered, setOffered] = useState<Sessions | null>(null);
  const [data, setData] = useState<Availability | null>(null);
  const [history, setHistory] = useState<StoredBooking[]>([]);
  const [error, setError] = useState("");
  const [historyError, setHistoryError] = useState("");
  const [notice, setNotice] = useState("");
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [refresh, setRefresh] = useState(0);
  const [passwords, setPasswords] = useState({
    currentPassword: "",
    newPassword: "",
  });
  const queryVersion = useRef(0);

  function invalidate() {
    queryVersion.current++;
    setData(null);
    setError("");
  }
  function reload() {
    invalidate();
    setPeriod("");
    setOffered(null);
    setRefresh((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    if (account.forcePasswordChange) {
      setLoading(false);
      return () => controller.abort();
    }
    setLoading(true);
    setOffered(null);
    setPeriod("");
    setData(null);
    if (!date) {
      setLoading(false);
      return () => controller.abort();
    }
    sessions(date, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return;
        setOffered(result);
        setPeriod(result.sessions[0]?.periodKey ?? "");
      })
      .catch((reason) => {
        if (!controller.signal.aborted)
          setError(
            reason instanceof Error
              ? reason.message
              : "Sessioni non disponibili",
          );
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [date, refresh, account.forcePasswordChange]);

  useEffect(() => {
    if (account.forcePasswordChange) return;
    let active = true;
    myBookings()
      .then((result) => {
        if (active) {
          setHistory(result);
          setHistoryError("");
        }
      })
      .catch(() => {
        if (active) setHistoryError("Cronologia non disponibile.");
      });
    return () => {
      active = false;
    };
  }, [refresh, account.forcePasswordChange]);

  async function search() {
    const version = ++queryVersion.current;
    setPending(true);
    setError("");
    setData(null);
    try {
      const result = await availability(date, period);
      if (version !== queryVersion.current) return;
      if (!availabilityMatches(result, offered?.sessions ?? [], date, period)) {
        reload();
        setError("Gli orari sono cambiati. Seleziona una sessione aggiornata.");
      } else setData(result);
    } catch (reason) {
      if (version === queryVersion.current) {
        if (reason instanceof ApiError && reason.status === 400) reload();
        setError(
          reason instanceof Error
            ? reason.message
            : "Disponibilita non disponibile",
        );
      }
    } finally {
      setPending(false);
    }
  }

  async function reserve(bayId: string) {
    if (
      !data ||
      !availabilityMatches(data, offered?.sessions ?? [], date, period)
    )
      return;
    setPending(true);
    setError("");
    setNotice("");
    try {
      await book({
        bayId,
        localDate: date,
        periodKey: period,
        request: "",
        expectedStartUtc: data.startUtc,
        expectedEndUtc: data.endUtc,
      });
      setNotice("Richiesta inviata, in attesa di approvazione.");
      reload();
    } catch (reason) {
      reload();
      setError(
        reason instanceof ApiError && reason.status === 409
          ? "La bay non e piu disponibile. Seleziona un altro turno."
          : reason instanceof Error
            ? reason.message
            : "Prenotazione non riuscita",
      );
    } finally {
      setPending(false);
    }
  }

  async function updatePassword(event: React.FormEvent) {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      await changePassword(passwords);
      setPasswords({ currentPassword: "", newPassword: "" });
      onPasswordChanged();
      setNotice("Password aggiornata.");
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : "Password non aggiornata",
      );
    } finally {
      setPending(false);
    }
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <h2>
          {account.forcePasswordChange
            ? "Aggiorna la password"
            : "Trova il tuo prossimo turno."}
        </h2>
        {!account.forcePasswordChange && (
          <p className="muted">{offered?.timeZone ?? "Orari del campo"}</p>
        )}
      </section>
      {account.forcePasswordChange && (
        <section className="admin-section">
          <h3>Aggiorna la password</h3>
          <form className="form-grid" onSubmit={updatePassword}>
            <label>
              Password attuale
              <input
                type="password"
                required
                autoComplete="current-password"
                value={passwords.currentPassword}
                onChange={(event) =>
                  setPasswords({
                    ...passwords,
                    currentPassword: event.target.value,
                  })
                }
              />
            </label>
            <label>
              Nuova password
              <input
                type="password"
                required
                minLength={12}
                maxLength={100}
                autoComplete="new-password"
                value={passwords.newPassword}
                onChange={(event) =>
                  setPasswords({
                    ...passwords,
                    newPassword: event.target.value,
                  })
                }
              />
            </label>
            <button disabled={pending}>Aggiorna password</button>
          </form>
        </section>
      )}
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {!account.forcePasswordChange && (
        <>
          <fieldset className="toolbar" disabled={pending}>
            <legend className="sr-only">Selezione del turno</legend>
            <label>
              Data
              <input
                type="date"
                required
                value={date}
                onChange={(event) => {
                  invalidate();
                  setDate(event.target.value);
                  setPeriod("");
                  setOffered(null);
                }}
              />
            </label>
            <label>
              Sessione
              <select
                disabled={loading || !offered?.sessions.length}
                value={period}
                onChange={(event) => {
                  invalidate();
                  setPeriod(event.target.value);
                }}
              >
                {!period && (
                  <option value="">
                    {loading ? "Caricamento..." : "Nessuna sessione"}
                  </option>
                )}
                {offered?.sessions.map((session) => (
                  <option key={session.periodKey} value={session.periodKey}>
                    {sessionLabel(session, offered.timeZone)}
                  </option>
                ))}
              </select>
            </label>
            <button
              disabled={loading || !period || account.forcePasswordChange}
              onClick={() => void search()}
            >
              Cerca disponibilita
            </button>
            <button className="secondary-button" onClick={reload}>
              Aggiorna sessioni
            </button>
          </fieldset>
          <div aria-live="polite">
            {loading && <p>Caricamento sessioni...</p>}
            {pending && <p>Operazione in corso...</p>}
            {!loading && offered && !offered.sessions.length && (
              <p>Nessuna sessione prenotabile per questa data.</p>
            )}
            {notice && <p className="success">{notice}</p>}
          </div>
          {data && (
            <section aria-label="Bay disponibili">
              <div className="section-heading">
                <h3>{data.bays.length} bay disponibili</h3>
              </div>
              {!data.bays.length && (
                <p>Nessuna bay disponibile per questo turno.</p>
              )}
              <div className="grid">
                {data.bays.map((bay) => (
                  <article className="bay" key={bay.id}>
                    <span className="bay-code">{bay.code}</span>
                    <h3>{bay.description || "Postazione operativa"}</h3>
                    <p>
                      {bay.mechanisms
                        .map((item) => `${item.quantity} ${item.type}`)
                        .join(" · ") || "Configurazione standard"}
                    </p>
                    <button
                      disabled={pending || account.forcePasswordChange}
                      onClick={() => void reserve(bay.id)}
                    >
                      Richiedi bay {bay.code}
                    </button>
                  </article>
                ))}
              </div>
            </section>
          )}
          <section className="admin-section">
            <h3>Le mie prenotazioni</h3>
            {historyError && <p role="alert">{historyError}</p>}
            {!historyError && !history.length && (
              <p className="muted">Nessuna prenotazione.</p>
            )}
            <ul className="record-list">
              {history.map((booking) => (
                <li key={booking.id}>
                  <span>
                    {booking.localDate} ·{" "}
                    {new Date(booking.startUtc).toLocaleString("it-IT", {
                      timeZone: offered?.timeZone ?? "Europe/Rome",
                    })}{" "}
                    -{" "}
                    {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                      timeZone: offered?.timeZone ?? "Europe/Rome",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </span>
                  <strong>{bookingStatus(booking.status)}</strong>
                </li>
              ))}
            </ul>
          </section>
        </>
      )}
      <p className="muted account-note">
        Account: {account.firstName} {account.lastName} ·{" "}
        {accountStatus(account.status)}
      </p>
    </Shell>
  );
}

export function App() {
  return <SessionGate />;
}
