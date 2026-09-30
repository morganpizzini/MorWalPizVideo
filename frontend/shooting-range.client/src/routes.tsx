import { useEffect, useState } from "react";
import { type RouteObject } from "react-router";
import { messages, openMessage } from "./api";
import { App, SessionGate, Shell } from "./App";
import { Admin } from "./Admin";
function Messages() {
  const [items, setItems] = useState<unknown[]>([]);
  const [text, setText] = useState("");
  const load = () => {
    void messages()
      .then(setItems)
      .catch(() => setItems([]));
  };
  useEffect(load, []);
  return (
    <section className="panel">
      <span className="eyebrow">CONTATTI</span>
      <h2>Messaggi</h2>
      <p className="muted">
        Apri una domanda per il team del campo. Gli admin vedono tutte le
        conversazioni.
      </p>
      <form
        onSubmit={async (e) => {
          e.preventDefault();
          await openMessage({ subject: "Richiesta dal portale", text });
          setText("");
          load();
        }}
      >
        <textarea
          required
          minLength={3}
          maxLength={2000}
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Come possiamo aiutarti?"
        />
        <button>Invia messaggio</button>
      </form>
      <div className="message-list">
        {items.map((item, index) => (
          <article className="message" key={index}>
            {JSON.stringify(item)}
          </article>
        ))}
      </div>
    </section>
  );
}
function MessagesRoute() {
  return (
    <SessionGate>
      {(props) => (
        <Shell
          account={props.account}
          onLogout={props.onLogout}
          loggingOut={props.loggingOut}
        >
          <Messages />
        </Shell>
      )}
    </SessionGate>
  );
}
function AdminRoute() {
  return (
    <SessionGate>
      {(props) =>
        props.account.isAdmin ? (
          <Admin {...props} />
        ) : (
          <Shell
            account={props.account}
            onLogout={props.onLogout}
            loggingOut={props.loggingOut}
          >
            <p role="alert">Account non autorizzato.</p>
          </Shell>
        )
      }
    </SessionGate>
  );
}
export const routes: RouteObject[] = [
  { path: "/", Component: App },
  { path: "/messages", Component: MessagesRoute },
  { path: "/admin", Component: AdminRoute },
];
