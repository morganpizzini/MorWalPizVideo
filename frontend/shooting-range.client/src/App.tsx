import { useCallback, useEffect, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { Link, NavLink, useFetcher, useNavigate } from "react-router";
import {
  accountStatus as formatAccountStatus,
  availabilityMatches,
  bookingStatus as formatBookingStatus,
  sessionLabel as formatSessionLabel,
} from "./schedule";
import {
  ApiError as RangeApiError,
  getSession,
  type Account,
  type Booking as StoredBooking,
  type Sessions,
} from "./api";
import { RangeDeferred, RangeSkeleton, RouteSessionError } from "./Loading";
import type {
  ActionResult,
  BookingRouteData,
  SessionErrorData,
} from "./routeData";

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
    <div className="range-shell">
      <header className="range-header">
        <Link className="range-brand" to="/" aria-label="MorWalPiz Shooting Range">
          <span className="range-brand__mark" aria-hidden="true">M</span>
          <span>MorWalPiz</span>
          <small>Shooting Range</small>
        </Link>
        {account && !account.forcePasswordChange && (
          <nav className="range-navigation" aria-label="Navigazione principale">
            <NavLink to="/" end>Prenota</NavLink>
            <NavLink to="/messages">Messaggi</NavLink>
            {account.isAdmin && <NavLink to="/admin">Amministrazione</NavLink>}
          </nav>
        )}
        {account && (
          <div className="range-account">
            <span className="range-account__name">
              {account.firstName} {account.lastName}
            </span>
            {onLogout && (
              <button
                type="button"
                className="button button-quiet"
                disabled={loggingOut}
                onClick={onLogout}
              >
                {loggingOut ? "Uscita..." : "Esci"}
              </button>
            )}
          </div>
        )}
      </header>
      <main className="range-main">{children}</main>
    </div>
  );
}

function Auth({
  onSubmit,
  pending,
  result,
  error,
  onRetry,
  onModeChange,
}: {
  onSubmit: (data: FormData) => void;
  pending: boolean;
  result?: ActionResult;
  error: string;
  onRetry: () => void;
  onModeChange: () => void;
}) {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [form, setForm] = useState({
    username: "",
    password: "",
    firstName: "",
    lastName: "",
  });
  const [notice, setNotice] = useState("");

  useEffect(() => {
    if (result?.intent === "register" && result.success) {
      setMode("login");
      setNotice(result.message);
      setForm((value) => ({ ...value, password: "" }));
    } else if (result?.intent === "register" && !result.success) {
      setNotice("");
    }
  }, [result]);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    data.set("intent", mode);
    onSubmit(data);
    setNotice("");
  }

  return (
    <section className="range-panel range-auth">
      <h1>{mode === "login" ? "Bentornato" : "Richiedi un account"}</h1>
      <p className="range-muted">
        Gli account nuovi restano in attesa dell'approvazione
        dell'amministratore.
      </p>
      <form onSubmit={submit}>
        <fieldset className="auth-fields" disabled={pending}>
          {mode === "register" && (
            <>
              <label>
                Nome
                <input
                  name="firstName"
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
                  name="lastName"
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
              name="username"
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
              name="password"
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
          <button type="submit" className="button button-primary">
            {pending
              ? "Attendi..."
              : mode === "login"
                ? "Accedi"
                : "Invia richiesta"}
          </button>
        </fieldset>
      </form>
      {error && (
        <p role="alert" className="range-error">
          {error}
          {error.includes("Sessione non disponibile") && (
            <button type="button" className="button button-quiet" onClick={onRetry}>
              Riprova
            </button>
          )}
        </p>
      )}
      {notice && <p role="status" className="range-success">{notice}</p>}
      <button
        type="button"
        disabled={pending}
        className="range-link-button"
        onClick={() => {
          setMode(mode === "login" ? "register" : "login");
          setNotice("");
          onModeChange();
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
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const navigate = useNavigate();
  const authFetcher = useFetcher<ActionResult>();
  const refreshAccount = useCallback(() => setRevision((value) => value + 1), []);

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
          if (!(reason instanceof RangeApiError && reason.status === 401)) {
            setError("Sessione non disponibile. Riprova.");
          }
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setRestoring(false);
      });
    return () => controller.abort();
  }, [revision]);

  useEffect(() => {
    const result = authFetcher.data;
    if (!result) return;
    if (result.intent === "login") {
      if (result.success && result.account) {
        setError("");
        setAccount(result.account);
      } else {
        setError(result.message);
      }
    } else if (result.intent === "register") {
      setError(result.success ? "" : result.message);
    } else if (result.intent === "logout") {
      if (result.success || result.status === 401) {
        setAccount(null);
        navigate("/");
      } else {
        setError(result.message || "Uscita non riuscita. Riprova.");
      }
    } else if (result.intent === "password-change" && result.success) {
      setRevision((value) => value + 1);
    }
  }, [authFetcher.data, navigate]);

  useEffect(() => {
    function expired() {
      setAccount(null);
      setRevision((value) => value + 1);
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

  function submitRootAction(data: FormData) {
    authFetcher.submit(data, { method: "post", action: "/" });
  }

  function signOut() {
    const data = new FormData();
    data.set("intent", "logout");
    submitRootAction(data);
  }

  if (restoring) {
    return (
      <Shell account={null}>
        <RangeSkeleton rows={2} label="Ripristino sessione..." />
      </Shell>
    );
  }
  if (!account) {
    return (
      <Shell account={null}>
        <Auth
          onSubmit={submitRootAction}
          pending={authFetcher.state !== "idle"}
          result={authFetcher.data}
          error={error}
          onRetry={() => setRevision((value) => value + 1)}
          onModeChange={() => setError("")}
        />
      </Shell>
    );
  }

  const props: SessionViewProps = {
    account,
    loggingOut: authFetcher.state !== "idle" && authFetcher.formData?.get("intent") === "logout",
    onLogout: signOut,
    onPasswordChanged: refreshAccount,
  };
  if (account.forcePasswordChange || !children) {
    return (
      <>
        {error && <p role="alert" className="range-error session-error">{error}</p>}
        <Booking {...props} />
      </>
    );
  }
  return (
    <>
      {error && <p role="alert" className="range-error session-error">{error}</p>}
      {children(props)}
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
  const [availabilityKey, setAvailabilityKey] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [passwords, setPasswords] = useState({
    currentPassword: "",
    newPassword: "",
  });
  const routeFetcher = useFetcher<BookingRouteData | SessionErrorData | null>();
  const availabilityFetcher = useFetcher<BookingRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const periodKey = `${date}|${period}`;
  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const availabilityError =
    availabilityFetcher.data && "sessionError" in availabilityFetcher.data
      ? availabilityFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data &&
    "date" in routeFetcher.data &&
    routeFetcher.data.date === date
      ? routeFetcher.data
      : null;
  const availabilityData =
    availabilityKey === periodKey &&
    availabilityFetcher.state !== "loading" &&
    availabilityFetcher.data &&
    "date" in availabilityFetcher.data &&
    availabilityFetcher.data.date === date &&
    availabilityFetcher.data.period === period
      ? availabilityFetcher.data
      : null;
  const pending = actionFetcher.state !== "idle";

  useEffect(() => {
    if (!account.forcePasswordChange) {
      routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
    }
  }, [account.forcePasswordChange, account.id, date]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result) return;
    if (result.intent === "book") {
      setError(result.success ? "" : result.message);
      setNotice(result.success ? result.message : "");
    } else if (result.intent === "password-change") {
      setError(result.success ? "" : result.message);
      setNotice(result.success ? result.message : "");
      if (result.success) {
        setPasswords({ currentPassword: "", newPassword: "" });
        onPasswordChanged();
      }
    }
  }, [actionFetcher.data, onPasswordChanged]);

  function search(selectedPeriod: string) {
    if (!selectedPeriod) return;
    setError("");
    setNotice("");
    setAvailabilityKey(`${date}|${selectedPeriod}`);
    availabilityFetcher.load(
      `/?date=${encodeURIComponent(date)}&period=${encodeURIComponent(selectedPeriod)}&search=1&region=availability`,
    );
  }

  function submitBooking(
    bayId: string,
    selectedPeriod: string,
    session: Sessions["sessions"][number],
  ) {
    if (pending || !session) return;
    const formData = new FormData();
    formData.set("intent", "book");
    formData.set("bayId", bayId);
    formData.set("localDate", date);
    formData.set("periodKey", selectedPeriod);
    formData.set("expectedStartUtc", session.startUtc);
    formData.set("expectedEndUtc", session.endUtc);
    actionFetcher.submit(formData, { method: "post", action: "/" });
  }

  function submitPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    formData.set("intent", "password-change");
    actionFetcher.submit(formData, { method: "post", action: "/" });
  }

  if (routeError) {
    return (
      <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => routeFetcher.load(`/?date=${encodeURIComponent(date)}`)}
        />
      </Shell>
    );
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <h1>{account.forcePasswordChange ? "Aggiorna la password" : "Prenotazioni"}</h1>
        {!account.forcePasswordChange && (
          <p className="range-muted">Scegli una data e una sessione per trovare una postazione.</p>
        )}
      </section>
      {account.forcePasswordChange ? (
        <section className="range-panel range-form-panel">
          <h2>Aggiorna la password</h2>
          <form className="form-grid" onSubmit={submitPassword}>
            <label>
              Password attuale
              <input
                name="currentPassword"
                type="password"
                required
                autoComplete="current-password"
                value={passwords.currentPassword}
                onChange={(event) => setPasswords({ ...passwords, currentPassword: event.target.value })}
              />
            </label>
            <label>
              Nuova password
              <input
                name="newPassword"
                type="password"
                required
                minLength={12}
                maxLength={100}
                autoComplete="new-password"
                value={passwords.newPassword}
                onChange={(event) => setPasswords({ ...passwords, newPassword: event.target.value })}
              />
            </label>
            <button className="button button-primary" disabled={pending}>Aggiorna password</button>
          </form>
        </section>
      ) : (
        <>
          {error && <p role="alert" className="range-error">{error}</p>}
          <fieldset className="toolbar" disabled={pending}>
            <legend className="sr-only">Selezione del turno</legend>
            <label>
              Data
              <input
                type="date"
                required
                value={date}
                onChange={(event) => {
                  setDate(event.target.value);
                  setPeriod("");
                  setAvailabilityKey("");
                  setError("");
                  setNotice("");
                }}
              />
            </label>
            <label>
              Sessione
              <select
                disabled={!routeData || routeFetcher.state === "loading"}
                value={period}
                onChange={(event) => {
                  setPeriod(event.target.value);
                  setAvailabilityKey("");
                }}
              >
                {(!routeData || !period) && (
                  <option value="">
                    {routeData ? "Seleziona una sessione" : "Caricamento..."}
                  </option>
                )}
                {routeData && routeFetcher.state !== "loading" && (
                  <RangeDeferred
                    resolve={routeData.offered}
                    fallback={null}
                  >
                    {(offered) => (
                      <OfferedSessionOptions
                        offered={offered}
                        selectedPeriod={period}
                        onDefaultPeriod={setPeriod}
                      />
                    )}
                  </RangeDeferred>
                )}
              </select>
            </label>
            <button
              type="button"
              className="button button-primary"
              disabled={!routeData || !period}
              onClick={() => search(period)}
            >
              Cerca disponibilità
            </button>
            <button
              type="button"
              className="button button-quiet"
              onClick={() => {
                setAvailabilityKey("");
                routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
              }}
            >
              Aggiorna sessioni
            </button>
          </fieldset>
          <div className="booking-feedback" aria-live="polite">
            {notice && <p role="status" className="range-success">{notice}</p>}
            {(!routeData || routeFetcher.state === "loading") && (
              <RangeSkeleton rows={2} label="Caricamento sessioni..." />
            )}
          </div>
          {routeData && routeFetcher.state !== "loading" && (
            <>
              <RangeDeferred
                resolve={routeData.offered}
                fallback={<RangeSkeleton rows={2} label="Caricamento sessioni..." />}
                onRetry={() => routeFetcher.load(`/?date=${encodeURIComponent(date)}`)}
              >
                {(offered) => (
                  <>
                    <p className="range-muted">{offered.timeZone}</p>
                    {!offered.sessions.length && (
                      <p className="range-empty">Nessuna sessione prenotabile per questa data.</p>
                    )}
                  </>
                )}
              </RangeDeferred>
              <section className="range-panel">
                <h2>Postazioni disponibili</h2>
                {availabilityError && (
                  <RouteSessionError
                    message={availabilityError.message ?? "Sessione non disponibile."}
                    status={availabilityError.status}
                    onRetry={() => search(period)}
                  />
                )}
                {!availabilityData && !availabilityError && availabilityKey === periodKey && (
                  <RangeSkeleton rows={3} label="Caricamento postazioni..." />
                )}
                {availabilityData && (
                  <RangeDeferred
                    resolve={availabilityData.availability}
                    fallback={<RangeSkeleton rows={3} label="Caricamento postazioni..." />}
                    onRetry={() => search(period)}
                  >
                    {(available) => {
                      if (!available) return null;
                      return (
                        <>
                          <p className="range-muted">{available.bays.length} postazioni disponibili</p>
                          {!available.bays.length && (
                            <p className="range-empty">Nessuna postazione disponibile per questo turno.</p>
                          )}
                          <div className="bay-grid">
                            {available.bays.map((bay) => (
                              <article className="bay" key={bay.id}>
                                <span className="bay-code">{bay.code}</span>
                                <h3>{bay.description || "Postazione operativa"}</h3>
                                <p>
                                  {bay.mechanisms.map((item) => `${item.quantity} ${item.type}`).join(" · ") || "Configurazione standard"}
                                </p>
                                <button
                                  type="button"
                                  className="button button-primary"
                                  disabled={pending}
                                  onClick={() => {
                                    void routeData.offered.then((offered) => {
                                      if (availabilityMatches(available, offered.sessions, date, period)) {
                                        const session = offered.sessions.find((item) => item.periodKey === period);
                                        if (session) submitBooking(bay.id, period, session);
                                      } else {
                                        setAvailabilityKey("");
                                        setError("Gli orari sono cambiati. Seleziona una sessione aggiornata.");
                                        routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
                                      }
                                    });
                                  }}
                                >
                                  Richiedi postazione {bay.code}
                                </button>
                              </article>
                            ))}
                          </div>
                        </>
                      );
                    }}
                  </RangeDeferred>
                )}
              </section>
              <BookingHistoryRegion
                history={routeData.history}
                offered={routeData.offered}
                onRetry={() => routeFetcher.load(`/?date=${encodeURIComponent(date)}`)}
              />
            </>
          )}
        </>
      )}
      <p className="range-muted account-note">
        Account: {account.firstName} {account.lastName} · {formatAccountStatus(account.status)}
      </p>
    </Shell>
  );
}

function BookingHistoryRegion({
  history,
  offered,
  onRetry,
}: {
  history: Promise<StoredBooking[]>;
  offered: BookingRouteData["offered"];
  onRetry: () => void;
}) {
  return (
    <section className="range-panel">
      <h2>Le mie prenotazioni</h2>
      <RangeDeferred
        resolve={offered}
        fallback={<RangeSkeleton rows={3} label="Caricamento prenotazioni..." />}
      >
        {(sessions) => (
          <RangeDeferred
            resolve={history}
            fallback={<RangeSkeleton rows={3} label="Caricamento prenotazioni..." />}
            onRetry={onRetry}
          >
            {(items: StoredBooking[]) => items.length ? (
              <ul className="record-list">
                {items.map((booking) => (
                  <li key={booking.id}>
                    <span>
                      {booking.localDate} ·{" "}
                      {new Date(booking.startUtc).toLocaleString("it-IT", {
                        timeZone: sessions.timeZone,
                      })}{" "}
                      -{" "}
                      {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                        timeZone: sessions.timeZone,
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </span>
                    <strong>{formatBookingStatus(booking.status)}</strong>
                  </li>
                ))}
              </ul>
            ) : <p className="range-empty">Nessuna prenotazione.</p>}
          </RangeDeferred>
        )}
      </RangeDeferred>
    </section>
  );
}

function OfferedSessionOptions({
  offered,
  selectedPeriod,
  onDefaultPeriod,
}: {
  offered: Sessions;
  selectedPeriod: string;
  onDefaultPeriod: (period: string) => void;
}) {
  useEffect(() => {
    if (!selectedPeriod && offered.sessions[0]) {
      onDefaultPeriod(offered.sessions[0].periodKey);
    }
  }, [offered, onDefaultPeriod, selectedPeriod]);

  return offered.sessions.map((session) => (
    <option key={session.periodKey} value={session.periodKey}>
      {formatSessionLabel(session, offered.timeZone)}
    </option>
  ));
}

export function App() {
  return <SessionGate />;
}
