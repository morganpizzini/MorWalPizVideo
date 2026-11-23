import { useEffect, useState, type FormEvent } from "react";
import { useFetcher } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import type { Booking } from "./api";
import { RangeSkeleton, RouteSessionError } from "./Loading";
import type {
  ActionResult,
  AdminBookingsRouteData,
  SessionErrorData,
} from "./routeData";
import { bookingStatus, sessionLabel } from "./schedule";

type IntervalGroup = {
  bayId: string;
  startUtc: string;
  endUtc: string;
  bookings: Booking[];
};
type DateGroup = { localDate: string; intervals: IntervalGroup[] };

export function groupBookings(bookings: Booking[]): DateGroup[] {
  const dates = new Map<string, Map<string, IntervalGroup>>();
  for (const booking of bookings) {
    let intervals = dates.get(booking.localDate);
    if (!intervals) {
      intervals = new Map();
      dates.set(booking.localDate, intervals);
    }
    const key = `${booking.bayId}\u0000${booking.startUtc}\u0000${booking.endUtc}`;
    let interval = intervals.get(key);
    if (!interval) {
      interval = {
        bayId: booking.bayId,
        startUtc: booking.startUtc,
        endUtc: booking.endUtc,
        bookings: [],
      };
      intervals.set(key, interval);
    }
    interval.bookings.push(booking);
  }
  return [...dates.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([localDate, intervals]) => ({
      localDate,
      intervals: [...intervals.values()].sort((left, right) =>
        left.bayId.localeCompare(right.bayId) || left.startUtc.localeCompare(right.startUtc),
      ),
    }));
}

export function AdminBookings({
  account,
  onLogout,
  loggingOut,
}: SessionViewProps) {
  const initialDate = new Date().toISOString().slice(0, 10);
  const [date, setDate] = useState(initialDate);
  const [period, setPeriod] = useState("");
  const [targetUserId, setTargetUserId] = useState("");
  const [bayId, setBayId] = useState("");
  const [requestText, setRequestText] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const routeFetcher = useFetcher<AdminBookingsRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const busy = actionFetcher.state !== "idle";
  const loading = routeFetcher.state !== "idle";

  function loadBookings(selectedDate: string, selectedPeriod = "") {
    const query = new URLSearchParams({ date: selectedDate });
    if (selectedPeriod) query.set("period", selectedPeriod);
    routeFetcher.load(`/admin/bookings?${query.toString()}`);
  }

  useEffect(() => {
    loadBookings(initialDate);
  }, [account.id, routeFetcher.load]);

  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data && "bookings" in routeFetcher.data &&
      routeFetcher.data.date === date && routeFetcher.data.period === period
      ? routeFetcher.data
      : null;

  useEffect(() => {
    if (!routeData || period || routeData.offered.sessions.length === 0) return;
    const firstPeriod = routeData.offered.sessions[0].periodKey;
    setPeriod(firstPeriod);
    loadBookings(date, firstPeriod);
  }, [routeData, period, date]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result || (result.intent !== "decide-booking" && result.intent !== "create-admin-booking")) return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) {
      if (result.intent === "create-admin-booking") {
        setTargetUserId("");
        setBayId("");
        setRequestText("");
      }
      loadBookings(date, period);
      window.dispatchEvent(new Event("range-admin-booking-changed"));
    } else if (result.intent === "create-admin-booking" && result.status === 409) {
      loadBookings(date, period);
    }
  }, [actionFetcher.data, date, period, routeFetcher.load]);

  useEffect(() => {
    if (routeData?.availability && !routeData.availability.bays.some((bay) => bay.id === bayId)) {
      setBayId("");
    }
  }, [routeData?.availability, bayId]);

  function decide(booking: Booking, approved: boolean) {
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", "decide-booking");
    formData.set("payload", JSON.stringify({ id: booking.id, approved }));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin/bookings" });
  }

  function createBooking(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy || !routeData || !targetUserId || !bayId) return;
    const selected = routeData.offered.sessions.find((session) => session.periodKey === period);
    if (!selected || !routeData.availability?.bays.some((bay) => bay.id === bayId)) {
      setError("L'intervallo o la postazione non sono più disponibili. Aggiorna la selezione.");
      loadBookings(date, period);
      return;
    }
    const formData = new FormData();
    formData.set("intent", "create-admin-booking");
    formData.set("payload", JSON.stringify({
      userId: targetUserId,
      bayId,
      localDate: date,
      periodKey: period,
      request: requestText,
      expectedStartUtc: selected.startUtc,
      expectedEndUtc: selected.endUtc,
    }));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin/bookings" });
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <div>
          <h1>Prenotazioni</h1>
          <p className="range-muted">Richieste e occupazione da ieri in avanti, raggruppate per data e intervallo.</p>
        </div>
      </section>
      {busy && <p className="range-pending" role="status">Aggiornamento in corso...</p>}
      {notice && <p className="range-success" role="status">{notice}</p>}
      {error && <p role="alert" className="range-error">{error}</p>}
      {routeError && (
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => loadBookings(date, period)}
        />
      )}
      {!routeError && !routeData && (
        <RangeSkeleton rows={4} label="Caricamento prenotazioni..." />
      )}
      {!routeError && routeData && (
        <>
          <section className="range-panel" aria-labelledby="create-booking-heading">
            <h2 id="create-booking-heading">Crea una prenotazione approvata</h2>
            <form className="admin-booking-form" onSubmit={createBooking}>
              <fieldset className="form-grid" disabled={busy || loading}>
                <label>
                  Utente
                  <select
                    required
                    value={targetUserId}
                    onChange={(event) => setTargetUserId(event.target.value)}
                  >
                    <option value="">Seleziona un utente</option>
                    {routeData.users.filter((user) => user.status === 1 && !user.forcePasswordChange).map((user) => (
                      <option key={user.id} value={user.id}>
                        {user.firstName} {user.lastName} (@{user.username})
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Data locale
                  <input
                    type="date"
                    required
                    value={date}
                    onChange={(event) => {
                      const nextDate = event.target.value;
                      setDate(nextDate);
                      setPeriod("");
                      setBayId("");
                      if (nextDate) loadBookings(nextDate);
                    }}
                  />
                </label>
                <label>
                  Sessione o intervallo
                  <select
                    required
                    value={period}
                    onChange={(event) => {
                      const nextPeriod = event.target.value;
                      setPeriod(nextPeriod);
                      setBayId("");
                      loadBookings(date, nextPeriod);
                    }}
                  >
                    <option value="">Seleziona un intervallo</option>
                    {routeData.offered.sessions.map((session) => (
                      <option key={session.periodKey} value={session.periodKey}>
                        {sessionLabel(session, routeData.config.timeZone)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Postazione
                  <select
                    required
                    value={bayId}
                    disabled={!routeData.availability?.bays.length}
                    onChange={(event) => setBayId(event.target.value)}
                  >
                    <option value="">Seleziona una postazione</option>
                    {routeData.availability?.bays.map((bay) => (
                      <option key={bay.id} value={bay.id}>{bay.code}</option>
                    ))}
                  </select>
                </label>
                <label className="wide-field">
                  Nota facoltativa (massimo 1000 caratteri)
                  <textarea
                    maxLength={1000}
                    value={requestText}
                    onChange={(event) => setRequestText(event.target.value)}
                  />
                </label>
                <button
                  type="submit"
                  className="button button-primary"
                  disabled={!routeData.availability?.bays.length || !targetUserId || !bayId}
                >
                  Crea e approva
                </button>
              </fieldset>
            </form>
            {period && routeData.availability?.bays.length === 0 && (
              <p role="status" className="range-empty">Intervallo non disponibile: scegli un altro turno o una postazione libera.</p>
            )}
            {!routeData.offered.sessions.length && (
              <p role="status" className="range-empty">Nessun intervallo prenotabile per questa data.</p>
            )}
          </section>
          <section className="range-panel" aria-labelledby="booking-list-heading">
            <h2 id="booking-list-heading">Occupazione e richieste</h2>
            {routeData.bookings.length ? (
              <div className="admin-booking-dates">
                {groupBookings(routeData.bookings).map((dateGroup) => (
                  <section key={dateGroup.localDate} aria-labelledby={`booking-date-${dateGroup.localDate}`}>
                    <h3 id={`booking-date-${dateGroup.localDate}`}>{dateGroup.localDate}</h3>
                    {dateGroup.intervals.map((interval) => {
                      const bay = routeData.bays.find((item) => item.id === interval.bayId);
                      const activeParticipants = interval.bookings.filter(
                        (booking) => booking.status === 0 || booking.status === 1,
                      ).length;
                      const start = new Date(interval.startUtc);
                      const end = new Date(interval.endUtc);
                      const time = `${start.toLocaleTimeString("it-IT", {
                        timeZone: routeData.config.timeZone,
                        hour: "2-digit",
                        minute: "2-digit",
                      })} - ${end.toLocaleTimeString("it-IT", {
                        timeZone: routeData.config.timeZone,
                        hour: "2-digit",
                        minute: "2-digit",
                      })}`;
                      return (
                        <section className="admin-booking-interval" key={`${interval.bayId}-${interval.startUtc}-${interval.endUtc}`}>
                          <h4>
                            Postazione {bay?.code ?? interval.bayId} · {time} · {activeParticipants} {activeParticipants === 1 ? "partecipante attivo" : "partecipanti attivi"}
                          </h4>
                          <ul className="record-list">
                            {interval.bookings.map((booking) => {
                              const user = routeData.users.find((item) => item.id === booking.userId);
                              const canDecide = new Date(booking.startUtc).getTime() > Date.now();
                              return (
                                <li key={booking.id}>
                                  <div>
                                    <strong>{user ? `${user.firstName} ${user.lastName} (@${user.username})` : booking.userId}</strong>
                                    <p>Stato: {bookingStatus(booking.status)}</p>
                                    {booking.request && <p>Nota: {booking.request}</p>}
                                  </div>
                                  {canDecide ? (
                                    <div className="action-group">
                                      {booking.status !== 1 && (
                                        <button
                                          type="button"
                                          className="button button-primary"
                                          disabled={busy}
                                          onClick={() => decide(booking, true)}
                                        >
                                          {booking.status === 3 ? "Ripristina e approva" : "Approva"}
                                        </button>
                                      )}
                                      {booking.status !== 2 && booking.status !== 3 && (
                                        <button
                                          type="button"
                                          className="button button-quiet"
                                          disabled={busy}
                                          onClick={() => decide(booking, false)}
                                        >
                                          Rifiuta
                                        </button>
                                      )}
                                    </div>
                                  ) : (
                                    <span className="range-muted">Sessione iniziata: non più gestibile</span>
                                  )}
                                </li>
                              );
                            })}
                          </ul>
                        </section>
                      );
                    })}
                  </section>
                ))}
              </div>
            ) : (
              <p className="range-empty">Nessuna prenotazione recente.</p>
            )}
          </section>
        </>
      )}
    </Shell>
  );
}
