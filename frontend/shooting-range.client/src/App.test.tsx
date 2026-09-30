// @vitest-environment jsdom
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SessionGate } from "./App";
import { getSession, logout, myBookings, sessions, type Account } from "./api";

vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  getSession: vi.fn(),
  logout: vi.fn(),
  myBookings: vi.fn(),
  sessions: vi.fn(),
}));

const account: Account = {
  id: "account",
  username: "account",
  firstName: "Test",
  lastName: "User",
  status: 1,
  isAdmin: false,
  forcePasswordChange: false,
};
let root: Root;
let container: HTMLDivElement;

beforeEach(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
  vi.resetAllMocks();
  vi.mocked(sessions).mockResolvedValue({
    localDate: "2030-01-12",
    timeZone: "Europe/Rome",
    sessions: [],
  });
  vi.mocked(myBookings).mockResolvedValue([]);
  container = document.createElement("div");
  document.body.append(container);
  root = createRoot(container);
});

afterEach(async () => {
  await act(async () => root.unmount());
  container.remove();
});

async function renderGate() {
  await act(async () =>
    root.render(
      <MemoryRouter>
        <SessionGate />
      </MemoryRouter>,
    ),
  );
}
async function click(label: string) {
  const button = Array.from(container.querySelectorAll("button")).find(
    (button) => button.textContent === label,
  );
  expect(button).toBeDefined();
  await act(async () => button!.click());
}

describe("Range session recovery", () => {
  it("shows no protected content until cookie session restoration completes", async () => {
    let resolveSession: (value: Account) => void = () => {};
    vi.mocked(getSession).mockReturnValue(
      new Promise((resolve) => {
        resolveSession = resolve;
      }),
    );
    await renderGate();
    expect(container.textContent).toContain("Ripristino sessione");
    expect(container.textContent).not.toContain("Le mie prenotazioni");
    expect(myBookings).not.toHaveBeenCalled();
    await act(async () => resolveSession(account));
    expect(container.textContent).toContain("Le mie prenotazioni");
    expect(myBookings).toHaveBeenCalledOnce();
  });

  it("forced-change sessions do not request domain data and can log out", async () => {
    vi.mocked(getSession).mockResolvedValue({
      ...account,
      forcePasswordChange: true,
    });
    vi.mocked(logout).mockResolvedValue(undefined);
    await renderGate();
    expect(container.textContent).toContain("Aggiorna la password");
    expect(container.textContent).not.toContain("Le mie prenotazioni");
    expect(sessions).not.toHaveBeenCalled();
    expect(myBookings).not.toHaveBeenCalled();
    expect(container.querySelector('a[href="/messages"]')).toBeNull();
    await click("Esci");
    expect(logout).toHaveBeenCalledOnce();
    expect(container.textContent).toContain("Bentornato");
    expect(container.textContent).not.toContain("Aggiorna la password");
  });

  it("keeps the signed-in state when logout fails and offers a retry", async () => {
    vi.mocked(getSession).mockResolvedValue({
      ...account,
      forcePasswordChange: true,
    });
    vi.mocked(logout)
      .mockRejectedValueOnce(new Error("network unavailable"))
      .mockResolvedValueOnce(undefined);
    await renderGate();
    await click("Esci");
    expect(container.textContent).toContain("Uscita non riuscita. Riprova.");
    expect(container.textContent).toContain("Aggiorna la password");
    await click("Esci");
    expect(container.textContent).toContain("Bentornato");
  });

  it("allows retry after restoration failure and clears content on session expiry", async () => {
    vi.mocked(getSession)
      .mockRejectedValueOnce(new Error("network unavailable"))
      .mockResolvedValueOnce(account);
    await renderGate();
    expect(container.textContent).toContain("Sessione non disponibile");
    await click("Riprova");
    expect(container.textContent).toContain("Le mie prenotazioni");
    await act(async () =>
      window.dispatchEvent(new Event("range-session-expired")),
    );
    expect(container.textContent).toContain("Bentornato");
    expect(container.textContent).not.toContain("Le mie prenotazioni");
  });
});
