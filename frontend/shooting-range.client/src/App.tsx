import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { Link, NavLink, useFetcher, useLocation, useNavigate } from "react-router";
import {
  accountStatus as formatAccountStatus,
  availabilityMatches,
  bookingStatus as formatBookingStatus,
  sessionLabel as formatSessionLabel,
} from "./schedule";
import {
  ApiError as RangeApiError,
  adminPendingBookingCount,
  getSession,
  type Account,
  type Booking as StoredBooking,
  type FriendBooking,
  type Sessions,
} from "./api";
import { OpeningDayDatePicker } from "./OpeningDayDatePicker";
import { RangeDeferred, RangeSkeleton, RouteSessionError } from "./Loading";
import type {
  ActionResult,
  BookingRouteData,
  SessionErrorData,
} from "./routeData";
import { AppearanceSelector, Dialog } from "@morwalpiz/layout";

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
  const [pendingRequestCount, setPendingRequestCount] = useState<number | null>(null);
  const [notificationError, setNotificationError] = useState(false);
  const [notificationRetry, setNotificationRetry] = useState(0);

  useEffect(() => {
    if (!account?.isAdmin || account.forcePasswordChange) {
      setPendingRequestCount(null);
      setNotificationError(false);
      return;
    }

    let active = true;
    let controller: AbortController | null = null;
    const refresh = () => {
      if (document.visibilityState === "hidden") return;
      controller?.abort();
      const requestController = new AbortController();
      controller = requestController;
      void adminPendingBookingCount(requestController.signal)
        .then(({ count }) => {
          if (!active || controller !== requestController) return;
          setPendingRequestCount(count);
          setNotificationError(false);
        })
        .catch(() => {
          if (!active || controller !== requestController || requestController.signal.aborted) return;
          setNotificationError(true);
        });
    };
    const refreshOnVisibility = () => {
      if (document.visibilityState === "visible") refresh();
    };
    const interval = window.setInterval(refreshOnVisibility, 60_000);
    window.addEventListener("focus", refresh);
    window.addEventListener("range-admin-booking-changed", refresh);
    document.addEventListener("visibilitychange", refreshOnVisibility);
    refresh();

    return () => {
      active = false;
      controller?.abort();
      window.clearInterval(interval);
      window.removeEventListener("focus", refresh);
      window.removeEventListener("range-admin-booking-changed", refresh);
      document.removeEventListener("visibilitychange", refreshOnVisibility);
    };
  }, [account?.id, account?.isAdmin, account?.forcePasswordChange, notificationRetry]);

  return (
    <div className="range-shell">
      <header className="range-header">
        <Link className="range-brand" to="/" aria-label="MorWalPiz Shooting Range">
          <span className="range-brand__mark" aria-hidden="true">M</span>
          <span>MorWalPiz</span>
          <small>Shooting Range</small>
        </Link>
        <AppearanceSelector
          label="Aspetto"
          systemLabel="Sistema"
          lightLabel="Chiaro"
          darkLabel="Scuro"
          persistenceErrorMessage="La preferenza di aspetto resta attiva solo per questa visita perché il browser non può salvarla."
        />
        {account && !account.forcePasswordChange && (
          <nav className="range-navigation" aria-label="Navigazione principale">
            <NavLink to="/" end>Prenota</NavLink>
            <NavLink to="/friends">Amici</NavLink>
            <NavLink to="/messages">Messaggi</NavLink>
            {account.isAdmin && (
              <>
                <NavLink to="/admin" end>Amministrazione</NavLink>
                <NavLink
                  to="/admin/bookings"
                  aria-label={
                    pendingRequestCount === null
                      ? notificationError
                        ? "Richieste di prenotazione: conteggio non disponibile"
                        : "Richieste di prenotazione"
                      : notificationError
                        ? `Richieste di prenotazione: ultimo conteggio ${pendingRequestCount}, non aggiornato`
                        : `Richieste di prenotazione: ${pendingRequestCount} da approvare`
                  }
                >
                  Richieste
                  {pendingRequestCount !== null && pendingRequestCount > 0 && (
                    <span className="range-notification-badge" aria-hidden="true">
                      {pendingRequestCount > 99 ? "99+" : pendingRequestCount}
                    </span>
                  )}
                </NavLink>
                {notificationError && (
                  <button
                    type="button"
                    className="range-notification-retry"
                    onClick={() => setNotificationRetry((value) => value + 1)}
                  >
                    Conteggio non aggiornato · Riprova
                  </button>
                )}
              </>
            )}
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
  error,
  onRetry,
}: {
  onSubmit: (data: FormData) => void;
  pending: boolean;
  error: string;
  onRetry: () => void;
}) {
  const [form, setForm] = useState({
    username: "",
    password: "",
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    data.set("intent", "login");
    onSubmit(data);
  }

  return (
    <section className="range-panel range-auth">
      <h1>Bentornato</h1>
      <p className="range-muted">Accedi con l'account fornito dalla tua amministrazione.</p>
      <form onSubmit={submit}>
        <div className="auth-fields">
          <label>
            Username
            <input
              name="username"
              required
              value={form.username}
              autoComplete="username"
              disabled={pending}
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
              minLength={7}
              type="password"
              value={form.password}
              autoComplete="current-password"
              disabled={pending}
              onChange={(event) =>
                setForm({ ...form, password: event.target.value })
              }
            />
          </label>
          <button
            type="submit"
            className="button button-primary"
            disabled={pending}
          >
            {pending ? "Attendi..." : "Accedi"}
          </button>
        </div>
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
  const location = useLocation();
  const locationRef = useRef(location.pathname);
  const accountRef = useRef(account);
  const handledAuthResultRef = useRef<ActionResult | null>(null);
  const sessionExpiredRef = useRef(false);
  const focusCheckRef = useRef<AbortController | null>(null);
  locationRef.current = location.pathname;
  accountRef.current = account;
  const authFetcher = useFetcher<ActionResult>();
  const refreshAccount = useCallback(() => setRevision((value) => value + 1), []);
  const cancelFocusCheck = useCallback(() => {
    focusCheckRef.current?.abort();
    focusCheckRef.current = null;
  }, []);
  const revalidateFocusedSession = useCallback(async () => {
    if (!accountRef.current || sessionExpiredRef.current || focusCheckRef.current) return;
    const controller = new AbortController();
    focusCheckRef.current = controller;
    try {
      const value = await getSession(controller.signal);
      if (controller.signal.aborted || sessionExpiredRef.current) return;
      setAccount(value);
      setError("");
    } catch (reason) {
      if (controller.signal.aborted) return;
      if (reason instanceof RangeApiError && reason.status === 401) {
        window.dispatchEvent(new Event("range-session-expired"));
      } else {
        setError("Sessione non disponibile. Riprova.");
      }
    } finally {
      if (focusCheckRef.current === controller) focusCheckRef.current = null;
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    setRestoring(true);
    setError("");
    getSession(controller.signal)
      .then((value) => {
        if (!controller.signal.aborted) {
          sessionExpiredRef.current = false;
          setAccount(value);
        }
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
    if (!result || handledAuthResultRef.current === result) return;
    handledAuthResultRef.current = result;
    if (result.intent === "login") {
      if (result.success && result.account) {
        cancelFocusCheck();
        sessionExpiredRef.current = false;
        setError("");
        setAccount(result.account);
        if (locationRef.current !== "/") navigate("/", { replace: true });
      } else {
        setError(result.message);
      }
    } else if (result.intent === "logout") {
      if (result.success || result.status === 401) {
        cancelFocusCheck();
        sessionExpiredRef.current = true;
        setAccount(null);
        navigate("/");
      } else {
        setError(result.message || "Uscita non riuscita. Riprova.");
      }
    } else if (result.intent === "password-change" && result.success) {
      setRevision((value) => value + 1);
    }
  }, [authFetcher.data, cancelFocusCheck, navigate]);

  useEffect(() => {
    function expired() {
      if (!accountRef.current || sessionExpiredRef.current) return;
      cancelFocusCheck();
      sessionExpiredRef.current = true;
      setAccount(null);
      setError("");
      if (locationRef.current !== "/") navigate("/", { replace: true });
    }
    function refresh() {
      void revalidateFocusedSession();
    }
    window.addEventListener("range-session-expired", expired);
    window.addEventListener("focus", refresh);
    return () => {
      window.removeEventListener("range-session-expired", expired);
      window.removeEventListener("focus", refresh);
      cancelFocusCheck();
    };
  }, [cancelFocusCheck, navigate, revalidateFocusedSession]);

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
          error={error}
          onRetry={() => setRevision((value) => value + 1)}
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
        {error && (
          <p role="alert" className="range-error session-error">
            {error}
            {account && (
              <button
                type="button"
                className="button button-quiet"
                onClick={() => void revalidateFocusedSession()}
              >
                Riprova controllo sessione
              </button>
            )}
          </p>
        )}
        <Booking {...props} />
      </>
    );
  }
  return (
    <>
      {error && (
        <p role="alert" className="range-error session-error">
          {error}
          <button
            type="button"
            className="button button-quiet"
            onClick={() => void revalidateFocusedSession()}
          >
            Riprova controllo sessione
          </button>
        </p>
      )}
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
  const [recap, setRecap] = useState<{
    bay: import("./api").Bay;
    friendBookings: FriendBooking[];
    period: string;
    session: Sessions["sessions"][number];
    request: string;
  } | null>(null);
  const recapInputRef = useRef<HTMLTextAreaElement>(null);
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
  const visibleAvailabilityError =
    availabilityKey === periodKey &&
    availabilityFetcher.state === "idle" &&
    availabilityError;
  const pending = actionFetcher.state !== "idle";

  const closeRecap = useCallback(() => {
    if (!pending) setRecap(null);
  }, [pending]);

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
      if (result.success) {
        setRecap(null);
        setAvailabilityKey("");
        routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
        window.dispatchEvent(new Event("range-admin-booking-changed"));
      } else if (result.status === 409) {
        setAvailabilityKey("");
        routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
      }
    } else if (result.intent === "cancel-booking") {
      setError(result.success ? "" : result.message);
      setNotice(result.success ? result.message : "");
      if (result.success) {
        setAvailabilityKey("");
        routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
        window.dispatchEvent(new Event("range-admin-booking-changed"));
      } else if (result.status === 404 || result.status === 409) {
        routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
      }
    } else if (result.intent === "password-change") {
      setError(result.success ? "" : result.message);
      setNotice(result.success ? result.message : "");
      if (result.success) {
        setPasswords({ currentPassword: "", newPassword: "" });
        onPasswordChanged();
      }
    }
  }, [actionFetcher.data, date, onPasswordChanged, routeFetcher.load]);

  const search = useCallback((selectedPeriod: string) => {
    if (!selectedPeriod) return;
    setError("");
    setNotice("");
    setAvailabilityKey(`${date}|${selectedPeriod}`);
    availabilityFetcher.load(
      `/?date=${encodeURIComponent(date)}&period=${encodeURIComponent(selectedPeriod)}&search=1&region=availability`,
    );
  }, [availabilityFetcher.load, date]);

  useEffect(() => {
    if (
      !routeData ||
      routeFetcher.state === "loading" ||
      !period ||
      !routeData.offered.sessions.some((session) => session.periodKey === period)
    ) return;
    search(period);
  }, [period, routeData, routeFetcher.state, search]);

  function submitBooking(
    bayId: string,
    selectedPeriod: string,
    session: Sessions["sessions"][number],
    requestText: string,
  ) {
    if (pending || !session) return;
    const formData = new FormData();
    formData.set("intent", "book");
    formData.set("bayId", bayId);
    formData.set("localDate", date);
    formData.set("periodKey", selectedPeriod);
    formData.set("expectedStartUtc", session.startUtc);
    formData.set("expectedEndUtc", session.endUtc);
    formData.set("request", requestText);
    actionFetcher.submit(formData, { method: "post", action: "/" });
  }

  function submitPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    formData.set("intent", "password-change");
    actionFetcher.submit(formData, { method: "post", action: "/" });
  }

  function cancelReservation(id: string) {
    if (pending || !window.confirm("Vuoi annullare questa prenotazione? La registrazione resterà visibile nello storico.")) return;
    const formData = new FormData();
    formData.set("intent", "cancel-booking");
    formData.set("id", id);
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
                minLength={7}
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
            {routeData ? (
              <OpeningDayDatePicker
                value={date}
                openingDays={routeData.config.openingDays}
                disabled={pending}
                onChange={(value) => {
                  setDate(value);
                  setPeriod("");
                  setAvailabilityKey("");
                  setError("");
                  setNotice("");
                }}
              />
            ) : <RangeSkeleton rows={1} label="Caricamento configurazione..." />}
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
                  <OfferedSessionOptions
                    offered={routeData.offered}
                    selectedPeriod={period}
                    onDefaultPeriod={setPeriod}
                  />
                )}
              </select>
            </label>
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
              <p className="range-muted">{routeData.offered.timeZone}</p>
              {!routeData.offered.sessions.length && (
                <p className="range-empty">Nessuna sessione prenotabile per questa data.</p>
              )}
              <section className="range-panel">
                <h2>Postazioni disponibili</h2>
                {visibleAvailabilityError && (
                  <RouteSessionError
                    message={visibleAvailabilityError.message ?? "Sessione non disponibile."}
                    status={visibleAvailabilityError.status}
                    onRetry={() => search(period)}
                  />
                )}
                {!availabilityData && !visibleAvailabilityError && availabilityKey === periodKey && (
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
                            {available.bays.map((bay) => {
                              const friendBookings = (available.friendBookings ?? [])
                                .filter((friend) => friend.bayId === bay.id);
                              return (
                                <article className="bay" key={bay.id}>
                                <span className="bay-code">{bay.code}</span>
                                <h3>{bay.description || "Postazione operativa"}</h3>
                                <ul className="bay-inventory" aria-label={`Dettagli postazione ${bay.code}`}>
                                  <li>Bersagli: {bay.targets}</li>
                                  <li>Paratie: {bay.partitions}</li>
                                  <li>Piastre: {bay.plates}</li>
                                  <li>Popper: {bay.pepper}</li>
                                  <li>
                                    Dimensioni: {bay.height !== null && bay.width !== null
                                      ? `${bay.height} × ${bay.width}`
                                      : "non specificate"}
                                  </li>
                                </ul>
                                <p className="bay-mechanisms">
                                  Meccanismi: {bay.mechanisms.map((item) => `${item.quantity} ${item.type}`).join(" · ") || "nessuno"}
                                </p>
                                {friendBookings.length ? (
                                  <p className="friend-slot">
                                    Slot condiviso con amici diretti:{" "}
                                    {friendBookings.map((friend) =>
                                      `@${friend.username} (${formatBookingStatus(friend.status)})`,
                                    ).join(", ")}.
                                  </p>
                                ) : null}
                                <button
                                  type="button"
                                  className="button button-primary"
                                  disabled={pending}
                                  onClick={() => {
                                    if (availabilityMatches(available, routeData.offered.sessions, date, period)) {
                                      const session = routeData.offered.sessions.find((item) => item.periodKey === period);
                                      if (session) {
                                        setRecap({
                                          bay,
                                          friendBookings,
                                          period,
                                          session,
                                          request: "",
                                        });
                                        setError("");
                                      }
                                    } else {
                                      setAvailabilityKey("");
                                      setError("Gli orari sono cambiati. Seleziona una sessione aggiornata.");
                                      routeFetcher.load(`/?date=${encodeURIComponent(date)}`);
                                    }
                                  }}
                                >
                                  {friendBookings.length
                                    ? `Richiedi di unirti a ${bay.code}`
                                    : `Richiedi postazione ${bay.code}`}
                                </button>
                                </article>
                              );
                            })}
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
                onCancel={cancelReservation}
                cancelling={pending}
              />
            </>
          )}
        </>
      )}
      <Dialog
        open={recap !== null}
        title="Conferma la richiesta"
        description="Controlla i dettagli prima di inviare la prenotazione."
        onClose={closeRecap}
        closeLabel="Annulla"
        initialFocusRef={recapInputRef}
        closeOnEscape={!pending}
        closeOnBackdrop={!pending}
      >
        {recap && routeData && (
          <div className="booking-recap">
            <p>
              <strong>Data:</strong>{" "}
              {new Date(`${date}T12:00:00Z`).toLocaleDateString("it-IT", {
                timeZone: "UTC",
                dateStyle: "long",
              })}
            </p>
            <p><strong>Orario:</strong> {formatSessionLabel(recap.session, routeData.offered.timeZone)}</p>
            <p><strong>Postazione:</strong> {recap.bay.code}</p>
            {recap.friendBookings.length ? (
              <p>
                <strong>Richiesta di unirti al turno condiviso con amici diretti:</strong>{" "}
                {recap.friendBookings.map((friend) =>
                  `@${friend.username} (${formatBookingStatus(friend.status)})`,
                ).join(", ")}
              </p>
            ) : null}
            <label>
              Nota facoltativa (massimo 1000 caratteri)
              <textarea
                ref={recapInputRef}
                maxLength={1000}
                value={recap.request}
                disabled={pending}
                onChange={(event) => setRecap({ ...recap, request: event.target.value })}
              />
            </label>
            {error && <p role="alert" className="range-error">{error}</p>}
            <div className="action-group">
              <button
                type="button"
                className="button button-quiet"
                disabled={pending}
                onClick={() => setRecap(null)}
              >
                Annulla
              </button>
              <button
                type="button"
                className="button button-primary"
                disabled={pending}
                onClick={() => submitBooking(recap.bay.id, recap.period, recap.session, recap.request)}
              >
                {pending ? "Invio..." : "Conferma e invia"}
              </button>
            </div>
          </div>
        )}
      </Dialog>
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
  onCancel,
  cancelling,
}: {
  history: Promise<StoredBooking[]>;
  offered: BookingRouteData["offered"];
  onRetry: () => void;
  onCancel: (id: string) => void;
  cancelling: boolean;
}) {
  return (
    <section className="range-panel">
      <h2>Le mie prenotazioni</h2>
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
                    timeZone: offered.timeZone,
                  })}{" "}
                  -{" "}
                  {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                    timeZone: offered.timeZone,
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </span>
                <strong>{formatBookingStatus(booking.status)}</strong>
                {(booking.status === 0 || booking.status === 1) &&
                  new Date(booking.startUtc).getTime() > Date.now() && (
                    <button
                      type="button"
                      className="button button-quiet"
                      disabled={cancelling}
                      onClick={() => onCancel(booking.id)}
                    >
                      Annulla prenotazione
                    </button>
                  )}
              </li>
            ))}
          </ul>
        ) : <p className="range-empty">Nessuna prenotazione.</p>}
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
