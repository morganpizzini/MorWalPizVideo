import { useEffect, useState } from "react";
import { useFetcher } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import type { AdminUser, Booking } from "./api";
import { RangeSkeleton, RouteSessionError } from "./Loading";
import type { AdminReportRouteData, SessionErrorData } from "./routeData";
import { bookingStatus } from "./schedule";

function localInterval(booking: Booking, timeZone: string): string {
  const start = new Date(booking.startUtc);
  const end = new Date(booking.endUtc);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return "Orario non disponibile";
  const formatter = new Intl.DateTimeFormat("it-IT", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
  });
  return `${formatter.format(start)}–${formatter.format(end)}`;
}

export function AdminReport({
  account,
  onLogout,
  loggingOut,
}: SessionViewProps) {
  const [userId, setUserId] = useState("");
  const [attendance, setAttendance] = useState<"all" | "show" | "no-show">("all");
  const [page, setPage] = useState(0);
  const routeFetcher = useFetcher<AdminReportRouteData | SessionErrorData | null>();

  function load(selectedUserId = userId, selectedAttendance = attendance, selectedPage = page) {
    const query = new URLSearchParams({ attendance: selectedAttendance });
    if (selectedUserId) query.set("userId", selectedUserId);
    if (selectedPage) query.set("page", String(selectedPage));
    routeFetcher.load(`/admin/reports?${query.toString()}`);
  }

  useEffect(() => {
    load();
  }, [account.id, routeFetcher.load]);

  const routeError = routeFetcher.data && "sessionError" in routeFetcher.data
    ? routeFetcher.data.sessionError
    : null;
  const data = routeFetcher.data && "users" in routeFetcher.data
    ? routeFetcher.data
    : null;
  const loading = routeFetcher.state !== "idle";

  function selectUser(value: string) {
    setUserId(value);
    setPage(0);
    load(value, attendance, 0);
  }

  function selectAttendance(value: "all" | "show" | "no-show") {
    setAttendance(value);
    setPage(0);
    load(userId, value, 0);
  }

  function changePage(nextPage: number) {
    setPage(nextPage);
    load(userId, attendance, nextPage);
  }

  const selectedUser = data?.users.find((user: AdminUser) => user.id === userId);

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <h1>Storico prenotazioni</h1>
        <p className="range-muted">
          Consulta tutte le prenotazioni di una persona. “Presenza non segnalata”
          non conferma che la persona abbia partecipato.
        </p>
      </section>
      {routeError && (
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => load()}
        />
      )}
      {!routeError && !data && <RangeSkeleton rows={3} label="Caricamento utenti..." />}
      {!routeError && data && (
        <>
          <section className="range-panel">
            <div className="admin-report-filters">
              <label>
                Utente
                <select
                  required
                  value={userId}
                  disabled={loading}
                  onChange={(event) => selectUser(event.target.value)}
                >
                  <option value="">Seleziona un utente</option>
                  {data.users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.firstName} {user.lastName} (@{user.username})
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Indicazione di presenza
                <select
                  value={attendance}
                  disabled={!userId || loading}
                  onChange={(event) => selectAttendance(event.target.value as typeof attendance)}
                >
                  <option value="all">Tutte</option>
                  <option value="show">Presenza non segnalata</option>
                  <option value="no-show">Mancata presenza</option>
                </select>
              </label>
            </div>
            {!userId && <p className="range-empty">Seleziona un utente per caricare lo storico.</p>}
            {data.reportError && (
              <p className="range-error" role="alert">
                {data.reportError.message ?? "Impossibile caricare lo storico."}
                <button type="button" className="button button-quiet" onClick={() => load()}>
                  Riprova
                </button>
              </p>
            )}
          </section>
          {userId && (
            <section className="range-panel" aria-live="polite">
              <h2>{selectedUser
                ? `Prenotazioni di ${selectedUser.firstName} ${selectedUser.lastName} (@${selectedUser.username})`
                : "Prenotazioni"}</h2>
              {loading && <RangeSkeleton rows={4} label="Caricamento storico..." />}
              {!loading && data.report && (
                <>
                  {data.report.items.length ? (
                    <div className="admin-report-table-wrap">
                      <table className="admin-report-table">
                        <caption className="sr-only">Storico completo delle prenotazioni selezionate</caption>
                        <thead>
                          <tr>
                            <th scope="col">Data locale</th>
                            <th scope="col">Orario</th>
                            <th scope="col">Postazione</th>
                            <th scope="col">Stato</th>
                            <th scope="col">Presenza</th>
                          </tr>
                        </thead>
                        <tbody>
                          {data.report.items.map((booking) => (
                            <tr key={booking.id}>
                              <td>{booking.localDate}</td>
                              <td>{localInterval(booking, data.config.timeZone)}</td>
                              <td>{booking.bayCode ?? booking.bayId}</td>
                              <td>{bookingStatus(booking.status)}</td>
                              <td>
                                {booking.isNoShow
                                  ? `Mancata presenza · ${booking.noShowDate ?? "data non disponibile"}`
                                  : "Presenza non segnalata"}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <p className="range-empty">Nessuna prenotazione corrisponde al filtro selezionato.</p>
                  )}
                  <div className="admin-report-pagination">
                    <p className="range-muted">
                      {data.report.totalCount} prenotazioni · pagina {data.report.page + 1} di{" "}
                      {Math.max(1, Math.ceil(data.report.totalCount / data.report.pageSize))}
                    </p>
                    <div className="action-group">
                      <button
                        type="button"
                        className="button button-quiet"
                        disabled={data.report.page === 0 || loading}
                        onClick={() => changePage(data.report!.page - 1)}
                      >
                        Precedente
                      </button>
                      <button
                        type="button"
                        className="button button-quiet"
                        disabled={(data.report.page + 1) * data.report.pageSize >= data.report.totalCount || loading}
                        onClick={() => changePage(data.report!.page + 1)}
                      >
                        Successiva
                      </button>
                    </div>
                  </div>
                </>
              )}
            </section>
          )}
        </>
      )}
    </Shell>
  );
}
