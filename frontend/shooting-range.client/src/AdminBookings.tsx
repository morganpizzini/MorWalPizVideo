import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { useFetcher } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import { adminNoShowAlerts, type Booking, type NoShowAlert } from "./api";
import { RangeSkeleton, RouteSessionError } from "./Loading";
import { Dialog } from "@morwalpiz/layout";
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

function fieldLocalToday(timeZone: string): string {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());
  const values = Object.fromEntries(parts.map(({ type, value }) => [type, value]));
  return `${values.year}-${values.month}-${values.day}`;
}

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
  const [historyDate, setHistoryDate] = useState("");
  const [period, setPeriod] = useState("");
  const [targetUserId, setTargetUserId] = useState("");
  const [bayId, setBayId] = useState("");
  const [requestText, setRequestText] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [cancelTarget, setCancelTarget] = useState<Booking | null>(null);
  const [recordNoShow, setRecordNoShow] = useState<boolean | null>(null);
  const [confirmExistingNoShowRemoval, setConfirmExistingNoShowRemoval] = useState(false);
  const [alerts, setAlerts] = useState<NoShowAlert[]>([]);
  const [alertsError, setAlertsError] = useState("");
  const [alertsLoading, setAlertsLoading] = useState(false);
  const alertsController = useRef<AbortController | null>(null);
  const routeFetcher = useFetcher<AdminBookingsRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const busy = actionFetcher.state !== "idle";
  const loading = routeFetcher.state !== "idle";

  function loadBookings(selectedDate: string, selectedPeriod = "", selectedHistoryDate = historyDate, historyPage = 0) {
    const query = new URLSearchParams({ date: selectedDate });
    if (selectedPeriod) query.set("period", selectedPeriod);
    if (selectedHistoryDate) query.set("historyDate", selectedHistoryDate);
    if (historyPage) query.set("historyPage", String(historyPage));
    routeFetcher.load(`/admin/bookings?${query.toString()}`);
  }

  useEffect(() => {
    loadBookings(initialDate);
  }, [account.id, routeFetcher.load]);

  const refreshAlerts = useCallback(() => {
    alertsController.current?.abort();
    const controller = new AbortController();
    alertsController.current = controller;
    setAlertsLoading(true);
    void adminNoShowAlerts(controller.signal)
      .then((items) => {
        if (controller.signal.aborted) return;
        setAlerts(items);
        setAlertsError("");
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        setAlertsError(reason instanceof Error ? reason.message : "Impossibile caricare gli avvisi.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setAlertsLoading(false);
      });
  }, []);

  useEffect(() => {
    const refresh = () => {
      if (document.visibilityState !== "hidden") refreshAlerts();
    };
    const onVisibility = () => {
      if (document.visibilityState === "visible") refresh();
    };
    refresh();
    window.addEventListener("focus", refresh);
    window.addEventListener("range-admin-booking-changed", refresh);
    document.addEventListener("visibilitychange", onVisibility);
    return () => {
      alertsController.current?.abort();
      window.removeEventListener("focus", refresh);
      window.removeEventListener("range-admin-booking-changed", refresh);
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, [account.id, refreshAlerts]);

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
    if (!historyDate && routeData?.historyDate) setHistoryDate(routeData.historyDate);
  }, [historyDate, routeData?.historyDate]);

  useEffect(() => {
    if (!routeData || period || routeData.offered.sessions.length === 0) return;
    const firstPeriod = routeData.offered.sessions[0].periodKey;
    setPeriod(firstPeriod);
    loadBookings(date, firstPeriod);
  }, [routeData, period, date]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result || ![
      "decide-booking",
      "create-admin-booking",
      "decide-booking-attendance",
      "cancel-admin-booking",
    ].includes(result.intent)) return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) {
      if (result.intent === "create-admin-booking") {
        setTargetUserId("");
        setBayId("");
        setRequestText("");
      }
      if (result.intent === "cancel-admin-booking") setCancelTarget(null);
      loadBookings(date, period, historyDate, routeData?.history.page ?? 0);
      window.dispatchEvent(new Event("range-admin-booking-changed"));
    } else if (result.status === 409 &&
      ["create-admin-booking", "decide-booking-attendance", "cancel-admin-booking"].includes(result.intent)) {
      if (result.intent === "cancel-admin-booking") setCancelTarget(null);
      loadBookings(date, period, historyDate, routeData?.history.page ?? 0);
    }
  }, [actionFetcher.data, date, historyDate, period, routeData?.history.page, routeFetcher.load]);

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

  function decideAttendance(booking: Booking, isNoShow: boolean) {
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", "decide-booking-attendance");
    formData.set("payload", JSON.stringify({ id: booking.id, isNoShow }));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin/bookings" });
  }

  function beginCancellation(booking: Booking) {
    setCancelTarget(booking);
    setRecordNoShow(null);
    setConfirmExistingNoShowRemoval(false);
  }

  function confirmCancellation() {
    if (busy || !cancelTarget || !routeData) return;
    const requiresNoShowChoice =
      routeData.bookingPolicy.minimumCancellationDays > 0 && cancelTarget.status === 1;
    if (requiresNoShowChoice && recordNoShow === null) return;
    if (cancelTarget.isNoShow && recordNoShow !== true && !confirmExistingNoShowRemoval) return;
    const formData = new FormData();
    formData.set("intent", "cancel-admin-booking");
    formData.set("payload", JSON.stringify({
      id: cancelTarget.id,
      policyRevision: routeData.bookingPolicy.revision,
      recordNoShow: requiresNoShowChoice ? recordNoShow : null,
      confirmExistingNoShowRemoval,
      expectedIsNoShow: cancelTarget.isNoShow === true,
      expectedNoShowDate: cancelTarget.noShowDate ?? null,
    }));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin/bookings" });
  }

  function changeHistoryDate(nextDate: string, page = 0) {
    setHistoryDate(nextDate);
    loadBookings(date, period, nextDate, page);
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
          <section className="range-panel" aria-labelledby="no-show-alerts-heading">
            <h2 id="no-show-alerts-heading">Avvisi di mancata presenza</h2>
            <p className="range-muted">
              Avvisi informativi: non limitano prenotazioni, approvazioni o rifiuti.
            </p>
            {alertsError ? (
              <p className="range-error" role="alert">
                Avvisi non disponibili: {alertsError}
                <button type="button" className="button button-quiet" onClick={refreshAlerts}>
                  Riprova
                </button>
              </p>
            ) : alertsLoading ? (
              <RangeSkeleton rows={2} label="Caricamento avvisi..." />
            ) : alerts.length ? (
              <ul className="record-list">
                {alerts.map((alert) => {
                  const user = routeData.users.find((item) => item.id === alert.userId);
                  return (
                    <li key={alert.userId}>
                      <strong>
                        {user ? `${user.firstName} ${user.lastName} (@${user.username})` : alert.userId}
                      </strong>
                      <ul>
                        {alert.rules.map((rule) => (
                          <li key={rule.ruleId}>
                            {rule.label}: {rule.count} su {rule.threshold} negli ultimi {rule.windowDays} giorni
                          </li>
                        ))}
                      </ul>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p className="range-empty">Nessun avviso attivo.</p>
            )}
          </section>
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
            {            routeData.bookings.length ? (
              <div className="admin-booking-dates">
                {routeData.bookings.length > 0 && (
                  <p className="range-muted">
                    Prenotazioni da ieri in avanti. Per date precedenti usa lo storico giornaliero qui sotto.
                  </p>
                )}
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
                              const canMarkAttendance =
                                booking.status === 1 && booking.localDate <= fieldLocalToday(routeData.config.timeZone);
                              const canClearCancelledMark = booking.status === 3 && booking.isNoShow === true;
                              const canCancel = booking.status === 0 || booking.status === 1;
                              return (
                                <li key={booking.id}>
                                  <div>
                                    <strong>{user ? `${user.firstName} ${user.lastName} (@${user.username})` : booking.userId}</strong>
                                    <p>Stato: {bookingStatus(booking.status)}</p>
                                    {booking.request && <p>Nota: {booking.request}</p>}
                                    <p>
                                      {booking.isNoShow
                                        ? `Mancata presenza · ${booking.noShowDate ?? "data non disponibile"}`
                                        : "Presenza non segnalata"}
                                    </p>
                                  </div>
                                  <div className="action-group">
                                    {canDecide && (
                                      <>
                                      {booking.status !== 1 && (
                                        <button
                                          type="button"
                                          className="button button-primary"
                                          disabled={busy || booking.isNoShow === true}
                                          onClick={() => decide(booking, true)}
                                        >
                                          {booking.status === 3 ? "Ripristina e approva" : "Approva"}
                                        </button>
                                      )}
                                      {booking.status === 0 && (
                                        <button
                                          type="button"
                                          className="button button-quiet"
                                          disabled={busy}
                                          onClick={() => decide(booking, false)}
                                        >
                                          Rifiuta
                                        </button>
                                      )}
                                      </>
                                    )}
                                    {(canMarkAttendance || canClearCancelledMark || canCancel) && (
                                      <div className="action-group">
                                        {(canMarkAttendance || canClearCancelledMark) && (
                                          <button
                                            type="button"
                                            className="button button-quiet"
                                            disabled={busy}
                                            onClick={() => decideAttendance(booking, !booking.isNoShow)}
                                          >
                                            {booking.isNoShow ? "Rimuovi no-show" : "Segna no-show"}
                                          </button>
                                        )}
                                        {canCancel && (
                                          <button
                                            type="button"
                                            className="button button-quiet"
                                            disabled={busy}
                                            onClick={() => beginCancellation(booking)}
                                          >
                                            Annulla
                                          </button>
                                        )}
                                      </div>
                                    )}
                                    {(booking.status === 0 || booking.status === 1) && (
                                      <button
                                        type="button"
                                        className="button button-quiet"
                                        disabled={busy}
                                        onClick={() => beginCancellation(booking)}
                                      >
                                        Annulla prenotazione
                                      </button>
                                    )}
                                    {canDecide && booking.status !== 0 && booking.status !== 1 &&
                                      !(booking.status === 3 && booking.isNoShow) && (
                                        <span className="range-muted">Sessione iniziata: non più gestibile</span>
                                      )}
                                  </div>
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
          <section className="range-panel" aria-labelledby="booking-history-heading">
            <div className="section-heading">
              <div>
                <h2 id="booking-history-heading">Storico per data locale</h2>
                <p className="range-muted">
                  Seleziona una data per consultare tutte le prenotazioni e correggere la presenza.
                </p>
              </div>
              <label>
                Data locale
                <input
                  type="date"
                  value={historyDate || routeData.historyDate}
                  disabled={loading || busy}
                  onChange={(event) => changeHistoryDate(event.target.value)}
                />
              </label>
            </div>
            {loading ? (
              <RangeSkeleton rows={3} label="Caricamento storico giornaliero..." />
            ) : routeData.history.items.length ? (
              <>
                <ul className="record-list">
                  {routeData.history.items.map((booking) => {
                    const user = routeData.users.find((item) => item.id === booking.userId);
                    const canMarkAttendance =
                      booking.status === 1 && booking.localDate <= fieldLocalToday(routeData.config.timeZone);
                    const canClearCancelledMark = booking.status === 3 && booking.isNoShow === true;
                    return (
                      <li key={booking.id}>
                        <div>
                          <strong>{user ? `${user.firstName} ${user.lastName} (@${user.username})` : booking.userId}</strong>
                          <p>
                            {booking.localDate} · Postazione {booking.bayCode ?? booking.bayId} ·{" "}
                            {new Date(booking.startUtc).toLocaleTimeString("it-IT", {
                              timeZone: routeData.config.timeZone,
                              hour: "2-digit",
                              minute: "2-digit",
                            })}–{new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                              timeZone: routeData.config.timeZone,
                              hour: "2-digit",
                              minute: "2-digit",
                            })}
                          </p>
                          <p>Stato: {bookingStatus(booking.status)}</p>
                          <p>
                            {booking.isNoShow
                              ? `Mancata presenza · ${booking.noShowDate ?? "data non disponibile"}`
                              : "Presenza non segnalata"}
                          </p>
                        </div>
                        {(canMarkAttendance || canClearCancelledMark) && (
                          <button
                            type="button"
                            className="button button-quiet"
                            disabled={busy}
                            onClick={() => decideAttendance(booking, !booking.isNoShow)}
                          >
                            {booking.isNoShow ? "Rimuovi no-show" : "Segna no-show"}
                          </button>
                        )}
                      </li>
                    );
                  })}
                </ul>
                <div className="admin-report-pagination">
                  <p className="range-muted">
                    {routeData.history.totalCount} prenotazioni · pagina {routeData.history.page + 1} di{" "}
                    {Math.max(1, Math.ceil(routeData.history.totalCount / routeData.history.pageSize))}
                  </p>
                  <div className="action-group">
                    <button
                      type="button"
                      className="button button-quiet"
                      disabled={routeData.history.page === 0 || loading || busy}
                      onClick={() => changeHistoryDate(historyDate || routeData.historyDate, routeData.history.page - 1)}
                    >
                      Precedente
                    </button>
                    <button
                      type="button"
                      className="button button-quiet"
                      disabled={(routeData.history.page + 1) * routeData.history.pageSize >= routeData.history.totalCount || loading || busy}
                      onClick={() => changeHistoryDate(historyDate || routeData.historyDate, routeData.history.page + 1)}
                    >
                      Successiva
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <p className="range-empty">Nessuna prenotazione per questa data.</p>
            )}
          </section>
        </>
      )}
      <Dialog
        open={cancelTarget !== null}
        title="Annulla prenotazione"
        description="La prenotazione resterà nello storico con lo stato Annullata."
        onClose={() => {
          if (!busy) setCancelTarget(null);
        }}
        closeLabel="Torna alle prenotazioni"
        closeOnEscape={!busy}
        closeOnBackdrop={!busy}
      >
        {cancelTarget && routeData && (
          <div className="booking-cancel-dialog">
            <p>
              <strong>Utente:</strong>{" "}
              {routeData.users.find((user) => user.id === cancelTarget.userId)?.username ?? cancelTarget.userId}
            </p>
            <p><strong>Data della prenotazione:</strong> {cancelTarget.localDate}</p>
            {routeData.bookingPolicy.minimumCancellationDays > 0 && cancelTarget.status === 1 ? (
              <fieldset>
                <legend>Registrare una mancata presenza per questa cancellazione?</legend>
                <p className="range-muted">
                  La data registrata sarà oggi ({fieldLocalToday(routeData.config.timeZone)}), anche se la prenotazione è futura.
                </p>
                <label className="check-label">
                  <input
                    type="radio"
                    name="record-cancellation-no-show"
                    checked={recordNoShow === true}
                    onChange={() => setRecordNoShow(true)}
                  />
                  Sì, registra la mancata presenza
                </label>
                <label className="check-label">
                  <input
                    type="radio"
                    name="record-cancellation-no-show"
                    checked={recordNoShow === false}
                    onChange={() => setRecordNoShow(false)}
                  />
                  No, annulla senza registrarla
                </label>
              </fieldset>
            ) : routeData.bookingPolicy.minimumCancellationDays > 0 ? (
              <p className="range-muted">
                La prenotazione è in attesa: non è possibile registrare una mancata presenza.
              </p>
            ) : null}
            {cancelTarget.isNoShow && (
              <div>
                <p role="status">
                  Mancata presenza già registrata per {cancelTarget.noShowDate ?? "data non disponibile"}.
                  {recordNoShow === true && " Confermando, la data sarà sostituita con oggi."}
                  {recordNoShow !== true && " Annullando, l'indicazione esistente sarà rimossa."}
                </p>
                {recordNoShow !== true && (
                  <label className="check-label">
                    <input
                      type="checkbox"
                      checked={confirmExistingNoShowRemoval}
                      onChange={(event) => setConfirmExistingNoShowRemoval(event.target.checked)}
                    />
                    Confermo la rimozione della mancata presenza esistente
                  </label>
                )}
              </div>
            )}
            {error && <p role="alert" className="range-error">{error}</p>}
            <div className="action-group">
              <button
                type="button"
                className="button button-quiet"
                disabled={busy}
                onClick={() => setCancelTarget(null)}
              >
                Indietro
              </button>
              <button
                type="button"
                className="button button-primary"
                disabled={busy ||
                  routeData.bookingPolicy.minimumCancellationDays > 0 &&
                    cancelTarget.status === 1 && recordNoShow === null ||
                  cancelTarget.isNoShow && recordNoShow !== true && !confirmExistingNoShowRemoval}
                onClick={confirmCancellation}
              >
                {busy ? "Annullamento..." : "Conferma annullamento"}
              </button>
            </div>
          </div>
        )}
      </Dialog>
    </Shell>
  );
}
