import { useEffect, useState } from "react";
import { useFetcher } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import type { Booking } from "./api";
import { RangeSkeleton, RouteSessionError } from "./Loading";
import type {
  ActionResult,
  AdminBookingsRouteData,
  SessionErrorData,
} from "./routeData";
import { bookingStatus } from "./schedule";

export function AdminBookings({
  account,
  onLogout,
  loggingOut,
}: SessionViewProps) {
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const routeFetcher = useFetcher<AdminBookingsRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const busy = actionFetcher.state !== "idle";

  useEffect(() => {
    routeFetcher.load("/admin/bookings");
  }, [account.id, routeFetcher.load]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result || result.intent !== "decide-booking") return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) {
      routeFetcher.load("/admin/bookings");
      window.dispatchEvent(new Event("range-admin-booking-changed"));
    }
  }, [actionFetcher.data, routeFetcher.load]);

  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data && "bookings" in routeFetcher.data
      ? routeFetcher.data
      : null;

  function decide(booking: Booking, approved: boolean) {
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", "decide-booking");
    formData.set("payload", JSON.stringify({ id: booking.id, approved }));
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/admin/bookings" });
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <div>
          <h1>Richieste di prenotazione</h1>
          <p className="range-muted">Valuta le prenotazioni ancora da gestire.</p>
        </div>
      </section>
      {busy && <p className="range-pending" role="status">Aggiornamento in corso...</p>}
      {notice && <p className="range-success" role="status">{notice}</p>}
      {error && <p role="alert" className="range-error">{error}</p>}
      {routeError && (
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => routeFetcher.load("/admin/bookings")}
        />
      )}
      {!routeError && !routeData && (
        <RangeSkeleton rows={4} label="Caricamento richieste..." />
      )}
      {!routeError && routeData && (
        <section className="range-panel">
          {routeData.bookings.length ? (
            <ul className="record-list">
              {routeData.bookings.map((booking) => {
                const canDecide = new Date(booking.startUtc).getTime() > Date.now();
                return (
                  <li key={booking.id}>
                    <div>
                      <strong>
                        {booking.localDate} · Postazione{" "}
                        {routeData.bays.find((bay) => bay.id === booking.bayId)?.code ?? booking.bayId}
                      </strong>
                      <p>
                        {new Date(booking.startUtc).toLocaleString("it-IT", {
                          timeZone: routeData.config.timeZone,
                        })}{" "}
                        -{" "}
                        {new Date(booking.endUtc).toLocaleTimeString("it-IT", {
                          timeZone: routeData.config.timeZone,
                          hour: "2-digit",
                          minute: "2-digit",
                        })}{" "}
                        · {bookingStatus(booking.status)}
                      </p>
                      <p>
                        {routeData.users.find((user) => user.id === booking.userId)?.username ?? booking.userId}
                        {" · "}{booking.request}
                      </p>
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
          ) : (
            <p className="range-empty">Nessuna richiesta recente.</p>
          )}
        </section>
      )}
    </Shell>
  );
}
