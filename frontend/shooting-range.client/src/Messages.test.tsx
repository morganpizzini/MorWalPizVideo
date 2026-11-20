import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import type { AdminUser, MessageThread } from "./api";
import { MessageThreadCard } from "./routes";

const thread: MessageThread = {
  id: "thread-1",
  creationDateTime: "2030-01-01T10:00:00Z",
  userId: "user-1",
  subject: "Richiesta dal portale",
  status: 1,
  messages: [{
    authorUserId: "user-1",
    fromAdmin: false,
    text: "Ciao\nHo bisogno di aiuto.",
    createdUtc: "2030-01-01T10:15:00Z",
  }],
};

const sender: AdminUser = {
  id: "user-1",
  creationDateTime: "2030-01-01T00:00:00Z",
  username: "sara",
  firstName: "Sara",
  lastName: "Bianchi",
  status: 1,
  isAdmin: false,
  forcePasswordChange: false,
};

describe("message thread rendering", () => {
  it("shows a member the message text, sent time and current status without admin controls", () => {
    const html = renderToStaticMarkup(
      <MessageThreadCard
        thread={thread}
        accountId="user-1"
        isAdmin={false}
        users={[]}
        busy={false}
        onDecision={vi.fn()}
      />,
    );

    expect(html).toContain("Ciao");
    expect(html).toContain("Ho bisogno di aiuto.");
    expect(html).toContain('dateTime="2030-01-01T10:15:00Z"');
    expect(html).toContain("Presa in carico");
    expect(html).not.toContain("Segna");
    expect(html).not.toContain("user-1");
    expect(html).not.toContain("[object Object]");
  });

  it("shows administrators the sender and lets them correct a completed decision", () => {
    const html = renderToStaticMarkup(
      <MessageThreadCard
        thread={{ ...thread, status: 2 }}
        accountId="admin-1"
        isAdmin
        users={[sender]}
        busy={false}
        onDecision={vi.fn()}
      />,
    );

    expect(html).toContain("Mittente: Sara Bianchi (@sara)");
    expect(html).toContain("Completata");
    expect(html).toContain('aria-label="Segna Sara Bianchi come presa in carico"');
    expect(html).toContain('aria-label="Segna Sara Bianchi come rifiutata"');
    expect(html).toContain('aria-pressed="true"');
  });

  it("does not claim an unknown legacy status is pending or offer decisions", () => {
    const html = renderToStaticMarkup(
      <MessageThreadCard
        thread={{ ...thread, status: 99 }}
        accountId="admin-1"
        isAdmin
        users={[]}
        busy={false}
        onDecision={vi.fn()}
      />,
    );

    expect(html).toContain("Stato non disponibile");
    expect(html).toContain("Mittente non disponibile");
    expect(html).not.toContain("In attesa");
    expect(html).not.toContain("role=\"group\"");
  });

  it("handles empty histories and malformed message dates", () => {
    const html = renderToStaticMarkup(
      <MessageThreadCard
        thread={{ ...thread, messages: [{ ...thread.messages![0], createdUtc: "invalid" }] }}
        accountId="user-1"
        isAdmin={false}
        users={[]}
        busy={false}
        onDecision={vi.fn()}
      />,
    );
    const emptyHtml = renderToStaticMarkup(
      <MessageThreadCard
        thread={{ ...thread, messages: [] }}
        accountId="user-1"
        isAdmin={false}
        users={[]}
        busy={false}
        onDecision={vi.fn()}
      />,
    );

    expect(html).toContain("Data non disponibile");
    expect(emptyHtml).toContain("Nessun messaggio disponibile.");
  });
});
