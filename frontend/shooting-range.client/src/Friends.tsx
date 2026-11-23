import { useEffect, useState, type FormEvent } from "react";
import { useFetcher } from "react-router";
import { Shell, type SessionViewProps } from "./App";
import type { Friend, FriendRequest } from "./api";
import { RangeSkeleton, RouteSessionError } from "./Loading";
import type { ActionResult, FriendsRouteData, SessionErrorData } from "./routeData";

function friendName(friend: Friend): string {
  return [friend.firstName, friend.lastName].filter(Boolean).join(" ") || friend.username;
}

function formatRequestDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Data non disponibile"
    : new Intl.DateTimeFormat("it-IT", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(date);
}

export function FriendList({
  items,
  busy,
  onRemove,
}: {
  items: Friend[];
  busy: boolean;
  onRemove: (friend: Friend) => void;
}) {
  if (!items.length) return <p className="range-empty">Non hai ancora amicizie accettate.</p>;
  return (
    <ul className="record-list" aria-label="Amici accettati">
      {items.map((friend) => (
        <li key={friend.id}>
          <div>
            <strong>{friendName(friend)}</strong>
            <p>@{friend.username}</p>
          </div>
          <button
            type="button"
            className="button button-quiet"
            disabled={busy}
            aria-label={`Rimuovi l'amicizia con @${friend.username}`}
            onClick={() => onRemove(friend)}
          >
            Rimuovi {friend.username}
          </button>
        </li>
      ))}
    </ul>
  );
}

export function FriendRequestList({
  items,
  direction,
  busy,
  onAccept,
  onDecline,
  onCancel,
}: {
  items: FriendRequest[];
  direction: "incoming" | "outgoing";
  busy: boolean;
  onAccept: (request: FriendRequest) => void;
  onDecline: (request: FriendRequest) => void;
  onCancel: (request: FriendRequest) => void;
}) {
  if (!items.length) {
    return (
      <p className="range-empty">
        {direction === "incoming"
          ? "Non hai richieste di amicizia in arrivo."
          : "Non hai richieste in attesa di risposta."}
      </p>
    );
  }
  return (
    <ul
      className="record-list"
      aria-label={direction === "incoming" ? "Richieste ricevute" : "Richieste inviate"}
    >
      {items.map((request) => (
        <li key={request.id}>
          <div>
            <strong>{friendName(request)}</strong>
            <p>@{request.username}</p>
            <p>
              {direction === "incoming"
                ? "In attesa della tua risposta · ricevuta"
                : "In attesa di accettazione · inviata"}{" "}
              <time dateTime={request.createdUtc}>{formatRequestDate(request.createdUtc)}</time>
            </p>
          </div>
          <div className="action-group" role="group" aria-label={`Richiesta di @${request.username}`}>
            {direction === "incoming" ? (
              <>
                <button
                  type="button"
                  className="button button-primary"
                  disabled={busy}
                  aria-label={`Accetta la richiesta di @${request.username}`}
                  onClick={() => onAccept(request)}
                >
                  Accetta
                </button>
                <button
                  type="button"
                  className="button button-quiet"
                  disabled={busy}
                  aria-label={`Rifiuta la richiesta di @${request.username}`}
                  onClick={() => onDecline(request)}
                >
                  Rifiuta
                </button>
              </>
            ) : (
              <button
                type="button"
                className="button button-quiet"
                disabled={busy}
                aria-label={`Annulla la richiesta inviata a @${request.username}`}
                onClick={() => onCancel(request)}
              >
                Annulla richiesta
              </button>
            )}
          </div>
        </li>
      ))}
    </ul>
  );
}

export function Friends({ account, onLogout, loggingOut }: SessionViewProps) {
  const [username, setUsername] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const routeFetcher = useFetcher<FriendsRouteData | SessionErrorData | null>();
  const actionFetcher = useFetcher<ActionResult>();
  const busy = actionFetcher.state !== "idle" || routeFetcher.state === "loading";

  useEffect(() => {
    routeFetcher.load("/friends");
  }, [account.id, routeFetcher.load]);

  useEffect(() => {
    const result = actionFetcher.data;
    if (
      !result ||
      ![
        "send-friend-request",
        "accept-friend-request",
        "decline-friend-request",
        "cancel-friend-request",
        "remove-friend",
      ].includes(result.intent)
    ) return;
    setError(result.success ? "" : result.message);
    setNotice(result.success ? result.message : "");
    if (result.success) {
      if (result.intent === "send-friend-request") setUsername("");
    }
    routeFetcher.load("/friends");
  }, [actionFetcher.data, routeFetcher.load]);

  const routeError =
    routeFetcher.data && "sessionError" in routeFetcher.data
      ? routeFetcher.data.sessionError
      : null;
  const routeData =
    routeFetcher.data && "friends" in routeFetcher.data
      ? routeFetcher.data
      : null;

  function submitAction(intent: string, field: "username" | "userId" | "requestId", value: string) {
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", intent);
    formData.set(field, value);
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/friends" });
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const formData = new FormData();
    formData.set("intent", "send-friend-request");
    formData.set("username", username);
    setError("");
    setNotice("");
    actionFetcher.submit(formData, { method: "post", action: "/friends" });
  }

  return (
    <Shell account={account} onLogout={onLogout} loggingOut={loggingOut}>
      <section className="booking-heading">
        <div>
          <h1>Amici</h1>
          <p className="range-muted">
            Invia una richiesta tramite username esatto. L'amicizia diventa attiva
            solo dopo l'accettazione; solo gli amici accettati possono condividere un turno.
          </p>
        </div>
      </section>
      {error && <p role="alert" className="range-error">{error}</p>}
      {notice && <p role="status" className="range-success">{notice}</p>}
      {routeError && (
        <RouteSessionError
          message={routeError.message ?? "Sessione non disponibile."}
          status={routeError.status}
          onRetry={() => routeFetcher.load("/friends")}
        />
      )}
      {!routeError && (
        <>
          <section className="range-panel range-form-panel" aria-labelledby="add-friend-heading">
            <h2 id="add-friend-heading">Invia richiesta di amicizia</h2>
            <p className="range-muted" id="friend-request-hint">
              La persona non comparirà tra i tuoi amici finché non avrà accettato.
            </p>
            <form className="friend-add-form" onSubmit={submit}>
              <label>
                Username esatto
                <input
                  name="username"
                  autoComplete="off"
                  maxLength={80}
                  required
                  aria-describedby="friend-request-hint"
                  value={username}
                  disabled={busy}
                  onChange={(event) => setUsername(event.target.value)}
                />
              </label>
              <button type="submit" className="button button-primary" disabled={busy}>
                {busy ? "Aggiornamento..." : "Invia richiesta"}
              </button>
            </form>
          </section>
          <section className="range-panel" aria-labelledby="friends-heading">
            <h2 id="friends-heading">Amici accettati</h2>
            {!routeData ? (
              <RangeSkeleton rows={2} label="Caricamento amici..." />
            ) : (
              <FriendList
                items={routeData.friends}
                busy={busy}
                onRemove={(friend) => submitAction("remove-friend", "userId", friend.id)}
              />
            )}
          </section>
          <section className="range-panel" aria-labelledby="incoming-friend-requests-heading">
            <h2 id="incoming-friend-requests-heading">Richieste ricevute</h2>
            {!routeData ? (
              <RangeSkeleton rows={2} label="Caricamento richieste ricevute..." />
            ) : (
              <FriendRequestList
                items={routeData.requests.incoming}
                direction="incoming"
                busy={busy}
                onAccept={(request) => submitAction("accept-friend-request", "requestId", request.id)}
                onDecline={(request) => submitAction("decline-friend-request", "requestId", request.id)}
                onCancel={(request) => submitAction("cancel-friend-request", "requestId", request.id)}
              />
            )}
          </section>
          <section className="range-panel" aria-labelledby="outgoing-friend-requests-heading">
            <h2 id="outgoing-friend-requests-heading">Richieste inviate</h2>
            {!routeData ? (
              <RangeSkeleton rows={2} label="Caricamento richieste inviate..." />
            ) : (
              <FriendRequestList
                items={routeData.requests.outgoing}
                direction="outgoing"
                busy={busy}
                onAccept={(request) => submitAction("accept-friend-request", "requestId", request.id)}
                onDecline={(request) => submitAction("decline-friend-request", "requestId", request.id)}
                onCancel={(request) => submitAction("cancel-friend-request", "requestId", request.id)}
              />
            )}
          </section>
        </>
      )}
    </Shell>
  );
}
