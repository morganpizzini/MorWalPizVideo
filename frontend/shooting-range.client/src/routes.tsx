import { useEffect, useState, type FormEvent } from "react";
import { type RouteObject, useFetcher } from "react-router";
import { App, SessionGate, Shell, type SessionViewProps } from "./App";
import { Admin } from "./Admin";
import { RangeDeferred, RangeSkeleton, RouteSessionError } from "./Loading";
import {
  adminAction,
  adminLoader,
  bookingAction,
  bookingLoader,
  messagesAction,
  messagesLoader,
  type ActionResult,
  type MessagesRouteData,
  type SessionErrorData,
} from "./routeData";

function Messages({
  account,
  onLogout,
  loggingOut,
}: SessionViewProps) {
  const [text, setText] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const routeFetcher = useFetcher<MessagesRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();

  useEffect(() => {
    routeFetcher.load("/messages");
  }, [account.id, routeFetcher.load]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (!result || result.intent !== "send-message") return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) setText("");
  }, [actionFetcher.data]);

  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data && "items" in routeFetcher.data
      ? routeFetcher.data
      : null;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (actionFetcher.state !== "idle") return;
    const data = new FormData(event.currentTarget);
    data.set("intent", "send-message");
    setError("");
    setNotice("");
    actionFetcher.submit(data, { method: "post", action: "/messages" });
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <h1>Messaggi</h1>
        <p className="range-muted">
          Apri una domanda per il team del campo. Gli admin vedono tutte le
          conversazioni.
        </p>
      </section>
      {routeError && (
        <RouteSessionError message={routeError.message ?? "Sessione non disponibile."} status={routeError.status} />
      )}
      {!routeError && (
        <section className="range-panel">
          <form onSubmit={submit}>
            <label>
              Messaggio
              <textarea
                name="text"
                required
                minLength={3}
                maxLength={2000}
                value={text}
                onChange={(event) => setText(event.target.value)}
                placeholder="Come possiamo aiutarti?"
              />
            </label>
            <button
              type="submit"
              className="button button-primary"
              disabled={actionFetcher.state !== "idle"}
            >
              {actionFetcher.state !== "idle" ? "Invio..." : "Invia messaggio"}
            </button>
          </form>
          {error && <p role="alert" className="range-error">{error}</p>}
          {notice && <p role="status" className="range-success">{notice}</p>}
          <div className="message-list" aria-live="polite">
            {!routeData && <RangeSkeleton rows={3} label="Caricamento messaggi..." />}
            {routeData && (
              <RangeDeferred
                resolve={routeData.items}
                fallback={<RangeSkeleton rows={3} label="Caricamento messaggi..." />}
                onRetry={() => routeFetcher.load("/messages")}
              >
                {(items) => items.length ? items.map((item, index) => (
                  <article className="message" key={index}>
                    {JSON.stringify(item)}
                  </article>
                )) : <p className="range-empty">Nessun messaggio.</p>}
              </RangeDeferred>
            )}
          </div>
        </section>
      )}
    </Shell>
  );
}

function MessagesRoute() {
  return <SessionGate>{(props) => <Messages {...props} />}</SessionGate>;
}

function AdminRoute() {
  return (
    <SessionGate>
      {(props) => props.account.isAdmin ? (
        <Admin {...props} />
      ) : (
        <Shell
          account={props.account}
          onLogout={props.onLogout}
          loggingOut={props.loggingOut}
        >
          <RouteSessionError message="Account non autorizzato." status={403} />
        </Shell>
      )}
    </SessionGate>
  );
}

export const routes: RouteObject[] = [
  { path: "/", Component: App, loader: bookingLoader, action: bookingAction },
  {
    path: "/messages",
    Component: MessagesRoute,
    loader: messagesLoader,
    action: messagesAction,
  },
  {
    path: "/admin",
    Component: AdminRoute,
    loader: adminLoader,
    action: adminAction,
  },
];
