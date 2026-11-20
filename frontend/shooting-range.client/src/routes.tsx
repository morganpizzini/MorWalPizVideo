import { useEffect, useState, type FormEvent } from "react";
import { type RouteObject, useFetcher } from "react-router";
import { App, SessionGate, Shell, type SessionViewProps } from "./App";
import { Admin } from "./Admin";
import { AdminBookings } from "./AdminBookings";
import { RangeDeferred, RangeSkeleton, RouteSessionError } from "./Loading";
import {
  adminAction,
  adminBookingsLoader,
  adminLoader,
  bookingAction,
  bookingLoader,
  messagesAction,
  messagesLoader,
  type ActionResult,
  type MessagesRouteData,
  type SessionErrorData,
} from "./routeData";
import type { AdminUser, MessageStatusDecision, MessageThread } from "./api";

const messageStatuses: readonly {
  value: MessageStatusDecision;
  label: string;
}[] = [
  { value: 1, label: "Presa in carico" },
  { value: 2, label: "Completata" },
  { value: 3, label: "Rifiutata" },
];

const messageStatusLabels: Record<number, string> = {
  0: "In attesa",
  1: "Presa in carico",
  2: "Completata",
  3: "Rifiutata",
};

function formatMessageDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Data non disponibile"
    : new Intl.DateTimeFormat("it-IT", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(date);
}

export function MessageThreadCard({
  thread,
  accountId,
  isAdmin,
  users,
  busy,
  onDecision,
}: {
  thread: MessageThread;
  accountId: string;
  isAdmin: boolean;
  users: AdminUser[];
  busy: boolean;
  onDecision: (id: string, status: MessageStatusDecision) => void;
}) {
  const sender = users.find((user) => user.id === thread.userId);
  const senderName = sender
    ? [sender.firstName, sender.lastName].filter(Boolean).join(" ") || sender.username
    : "Mittente non disponibile";
  const knownStatus = typeof thread.status === "number"
    ? messageStatusLabels[thread.status]
    : undefined;
  const threadMessages = Array.isArray(thread.messages) ? thread.messages : [];

  return (
    <article className="message" aria-labelledby={`message-title-${thread.id}`}>
      <header className="message__header">
        <h2 id={`message-title-${thread.id}`}>{thread.subject || "Richiesta senza titolo"}</h2>
        <p className="message__status">
          Stato: <strong>{knownStatus ?? "Stato non disponibile"}</strong>
        </p>
      </header>
      {isAdmin && (
        <p className="message__sender">
          Mittente: {senderName}
          {sender && <> (@{sender.username})</>}
        </p>
      )}
      <div className="message__entries">
        {threadMessages.length ? threadMessages.map((message, index) => (
          <section
            className="message__entry"
            key={`${message.authorUserId}-${message.createdUtc}-${index}`}
          >
            <div className="message__entry-header">
              <strong>
                {message.fromAdmin
                  ? "Team Shooting Range"
                  : isAdmin
                    ? senderName
                    : message.authorUserId === accountId ? "Tu" : "Utente"}
              </strong>
              <time dateTime={message.createdUtc}>{formatMessageDate(message.createdUtc)}</time>
            </div>
            <p className="message__text">{message.text}</p>
          </section>
        )) : (
          <p className="message__empty">Nessun messaggio disponibile.</p>
        )}
      </div>
      {isAdmin && knownStatus && (
        <div className="message__decisions" role="group" aria-label={`Aggiorna stato per ${senderName}`}>
          {messageStatuses.map(({ value, label }) => (
            <button
              type="button"
              className={thread.status === value ? "button button-primary" : "button button-quiet"}
              key={value}
              aria-label={`Segna ${senderName} come ${label.toLowerCase()}`}
              aria-pressed={thread.status === value}
              disabled={busy || thread.status === value}
              onClick={() => onDecision(thread.id, value)}
            >
              {label}
            </button>
          ))}
        </div>
      )}
    </article>
  );
}

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
    if (!result || !["send-message", "decide-message-status"].includes(result.intent)) return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) {
      if (result.intent === "send-message") setText("");
      routeFetcher.load("/messages");
    }
  }, [actionFetcher.data, routeFetcher.load]);

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

  function decide(threadId: string, status: MessageStatusDecision) {
    if (actionFetcher.state !== "idle") return;
    const data = new FormData();
    data.set("intent", "decide-message-status");
    data.set("payload", JSON.stringify({ id: threadId, status }));
    setError("");
    setNotice("");
    actionFetcher.submit(data, { method: "post", action: "/messages" });
  }

  const busy = actionFetcher.state !== "idle";

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
          <form onSubmit={submit} className="mb-2">
            <label>
              Messaggio
              <textarea
                name="text"
                required
                minLength={3}
                maxLength={2000}
                disabled={busy}
                value={text}
                onChange={(event) => setText(event.target.value)}
                placeholder="Come possiamo aiutarti?"
              />
            </label>
            <button
              type="submit"
              className="button button-primary"
              disabled={busy}
            >
              {busy ? "Aggiornamento..." : "Invia messaggio"}
            </button>
          </form>
          {error && <p role="alert" className="range-error">{error}</p>}
          {notice && <p role="status" className="range-success">{notice}</p>}
          <div className="message-list" aria-live="polite">
            <p className="range-muted">Orari visualizzati nel fuso locale del dispositivo.</p>
            {!routeData && <RangeSkeleton rows={3} label="Caricamento messaggi..." />}
            {routeData && (
              <RangeDeferred
                resolve={routeData.items}
                fallback={<RangeSkeleton rows={3} label="Caricamento messaggi..." />}
                onRetry={() => routeFetcher.load("/messages")}
              >
                {({ threads, users }) => threads.length ? threads.map((thread) => (
                  <MessageThreadCard
                    key={thread.id}
                    thread={thread}
                    accountId={account.id}
                    isAdmin={account.isAdmin}
                    users={users}
                    busy={busy}
                    onDecision={decide}
                  />
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

function AdminBookingsRoute() {
  return (
    <SessionGate>
      {(props) => props.account.isAdmin ? (
        <AdminBookings {...props} />
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
  {
    path: "/admin/bookings",
    Component: AdminBookingsRoute,
    loader: adminBookingsLoader,
    action: adminAction,
  },
];
