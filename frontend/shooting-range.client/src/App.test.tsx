// @vitest-environment jsdom
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SessionGate } from "./App";
import { getSession, logout, myBookings, sessions, type Account } from "./api";

const testDoubles = vi.hoisted(() => ({ logout: vi.fn() }));

vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  getSession: vi.fn(),
  logout: testDoubles.logout,
  myBookings: vi.fn(),
  sessions: vi.fn(),
}));

vi.mock("react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router")>();
  const React = await import("react");
  return {
    ...actual,
    useFetcher: () => {
      const [data, setData] = React.useState<
        { intent: string; success: boolean; message: string } | undefined
      >();
      const [state, setState] = React.useState<"idle" | "submitting">("idle");
      const [formData, setFormData] = React.useState<FormData | undefined>();
      return {
        data,
        state,
        formData,
        load: () => {},
        submit: (submitted: FormData) => {
          setFormData(submitted);
          setState("submitting");
          void testDoubles.logout()
            .then(() => setData({ intent: "logout", success: true, message: "" }))
            .catch(() => setData({
              intent: "logout",
              success: false,
              message: "Uscita non riuscita. Riprova.",
            }))
            .finally(() => setState("idle"));
        },
      };
    },
  };
});

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
  it("disables authentication fields while a request is pending", async () => {
    vi.mocked(getSession).mockRejectedValue(new Error("not authenticated"));
    let finishSubmit: () => void = () => {};
    testDoubles.logout.mockReturnValue(
      new Promise<void>((resolve) => {
        finishSubmit = resolve;
      }),
    );
    await renderGate();

    await act(async () => {
      container
        .querySelector("form")!
        .dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
    });

    const inputs = container.querySelectorAll<HTMLInputElement>("input");
    expect(inputs).toHaveLength(2);
    expect(
      Array.from(inputs).every((input) => input.disabled),
    ).toBe(true);
    expect(
      container.querySelector<HTMLButtonElement>('button[type="submit"]')
        ?.disabled,
    ).toBe(true);

    await act(async () => finishSubmit());
  });

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
    expect(container.textContent).toContain("Scegli una data e una sessione");
    expect(myBookings).not.toHaveBeenCalled();
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

  it("requires seven characters in the account password field", async () => {
    vi.mocked(getSession).mockRejectedValue(new Error("not authenticated"));
    await renderGate();

    expect(container.querySelector<HTMLInputElement>('input[name="password"]')?.minLength).toBe(7);
    expect(container.querySelector(".mpv-appearance__select")).not.toBeNull();
  });

  it("requires seven characters for new passwords", async () => {
    vi.mocked(getSession).mockResolvedValue({
      ...account,
      forcePasswordChange: true,
    });
    await renderGate();

    expect(container.querySelector<HTMLInputElement>('input[name="newPassword"]')?.minLength).toBe(7);
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
      .mockResolvedValueOnce(account)
      .mockRejectedValueOnce(new Error("session expired"));
    await renderGate();
    expect(container.textContent).toContain("Sessione non disponibile");
    await click("Riprova");
    expect(container.textContent).toContain("Scegli una data e una sessione");
    await act(async () =>
      window.dispatchEvent(new Event("range-session-expired")),
    );
    expect(container.textContent).toContain("Bentornato");
    expect(container.textContent).not.toContain("Le mie prenotazioni");
  });
});
