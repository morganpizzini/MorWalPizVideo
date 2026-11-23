// @vitest-environment jsdom
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Booking, SessionGate, Shell } from "./App";
import {
  adminPendingBookingCount,
  ApiError,
  getSession,
  logout,
  myBookings,
  sessions,
  type Account,
} from "./api";
import type { BookingRouteData } from "./routeData";

const testDoubles = vi.hoisted(() => ({
  login: vi.fn(),
  logout: vi.fn(),
  adminPendingBookingCount: vi.fn(),
  fetcherLoad: vi.fn(),
  fetcherSubmit: vi.fn(),
  fetcherData: null as unknown,
}));

vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  getSession: vi.fn(),
  login: testDoubles.login,
  logout: testDoubles.logout,
  adminPendingBookingCount: testDoubles.adminPendingBookingCount,
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
        { intent: string; success: boolean; message: string; account?: Account; status?: number } | undefined
      >();
      const [state, setState] = React.useState<"idle" | "submitting">("idle");
      const [formData, setFormData] = React.useState<FormData | undefined>();
      return {
        data: data ?? testDoubles.fetcherData,
        state,
        formData,
        load: (href: string) => testDoubles.fetcherLoad(href),
        submit: (submitted: FormData) => {
          testDoubles.fetcherSubmit(submitted);
          setFormData(submitted);
          setState("submitting");
          const intent = submitted.get("intent");
          const operation = intent === "login"
            ? testDoubles.login(submitted)
            : intent === "logout" ? testDoubles.logout() : Promise.resolve();
          void Promise.resolve(operation)
            .then((result) => setData({
              intent: typeof intent === "string" ? intent : "",
              success: true,
              message: "",
              ...(intent === "login" ? { account: result } : {}),
            }))
            .catch((reason: unknown) => setData({
              intent: typeof intent === "string" ? intent : "",
              success: false,
              message: intent === "logout"
                ? "Uscita non riuscita. Riprova."
                : reason instanceof Error ? reason.message : "Operazione non riuscita.",
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
  testDoubles.fetcherData = null;
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
  await act(async () => Promise.resolve());
}
async function click(label: string) {
  const button = Array.from(document.querySelectorAll("button")).find(
    (button) => button.textContent === label,
  );
  expect(button).toBeDefined();
  await act(async () => button!.click());
}
async function submitLogin() {
  const form = container.querySelector("form")!;
  const setValue = Object.getOwnPropertyDescriptor(
    HTMLInputElement.prototype,
    "value",
  )!.set!;
  setValue.call(form.querySelector<HTMLInputElement>('input[name="username"]')!, "account");
  setValue.call(form.querySelector<HTMLInputElement>('input[name="password"]')!, "password");
  await act(async () => {
    form.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
  });
}

describe("Range session recovery", () => {
  it("opens a recap, cancels without sending, then confirms with the optional note", async () => {
    const date = new Date().toISOString().slice(0, 10);
    const session = {
      periodKey: "morning",
      startUtc: `${date}T08:00:00Z`,
      endUtc: `${date}T12:00:00Z`,
    };
    const mixedBlock = {
      periodKey: "09:00-11:00",
      startUtc: `${date}T08:00:00Z`,
      endUtc: `${date}T10:00:00Z`,
    };
    const bay = {
      id: "bay-1",
      creationDateTime: "",
      code: "01",
      description: "Bay",
      mechanisms: [],
      targets: 0,
      partitions: 0,
      plates: 0,
      pepper: 0,
      height: null,
      width: null,
      status: 0,
      reservedUntilUtc: null,
      whitelistUserIds: [],
    };
    testDoubles.fetcherData = {
      date,
      period: "morning",
      offered: {
        localDate: date,
        timeZone: "Europe/Rome",
        sessions: [session, mixedBlock],
      },
      config: {
        id: "default",
        creationDateTime: "",
        name: "Pepperbox",
        timeZone: "Europe/Rome",
        sessionMode: 3,
        openingDays: [0, 1, 2, 3, 4, 5, 6],
        morningStart: "09:00:00",
        morningEnd: "13:00:00",
        afternoonStart: "14:00:00",
        afternoonEnd: "18:00:00",
        hourlyMinutes: 120,
        continuousStart: null,
        continuousEnd: null,
        reservedReleaseDaysBefore: 2,
      },
      history: Promise.resolve([]),
      availability: Promise.resolve({
        localDate: date,
        periodKey: "morning",
        startUtc: session.startUtc,
        endUtc: session.endUtc,
        bays: [bay, { ...bay, id: "bay-2", code: "02" }],
        friendBookings: [
          { bayId: "bay-1", username: "direct-friend", status: 0 },
          { bayId: "bay-1", username: "second-friend", status: 1 },
          { bayId: "bay-1", username: "third-friend", status: 1 },
          { bayId: "bay-1", username: "fourth-friend", status: 0 },
        ],
      }),
    };
    await act(async () => root.render(
      <MemoryRouter>
        <Booking
          account={account}
          onLogout={() => {}}
          loggingOut={false}
          onPasswordChanged={() => {}}
        />
      </MemoryRouter>,
    ));
    await act(async () => Promise.resolve());
    await act(async () => Promise.resolve());

    expect(container.querySelector("select")?.textContent).toContain("09:00 - 11:00");
    const bayCards = container.querySelectorAll(".bay");
    expect(bayCards[0].textContent).toContain("Slot condiviso con amici diretti:");
    expect(bayCards[0].textContent).toContain("@direct-friend (In attesa)");
    expect(bayCards[0].textContent).toContain("@second-friend (Approvata)");
    expect(bayCards[0].textContent).toContain("@third-friend (Approvata)");
    expect(bayCards[0].textContent).toContain("@fourth-friend (In attesa)");
    expect(bayCards[0].textContent).toContain("Richiedi di unirti a 01");
    expect(bayCards[1].textContent).not.toContain("direct-friend");
    expect(bayCards[1].textContent).toContain("Richiedi postazione 02");
    await click("Richiedi di unirti a 01");
    await act(async () => Promise.resolve());
    expect(document.body.querySelector('[role="dialog"]')?.textContent).toContain("Data:");
    expect(document.body.querySelector('[role="dialog"]')?.textContent).toContain("Orario:");
    expect(document.body.querySelector('[role="dialog"]')?.textContent).toContain("Postazione: 01");
    expect(document.body.querySelector('[role="dialog"]')?.textContent).toContain("@direct-friend");
    expect(document.body.querySelector('[role="dialog"]')?.textContent).toContain("@fourth-friend");
    expect(testDoubles.fetcherSubmit).not.toHaveBeenCalled();

    await click("Annulla");
    expect(document.body.querySelector('[role="dialog"]')).toBeNull();
    expect(testDoubles.fetcherSubmit).not.toHaveBeenCalled();

    await click("Richiedi di unirti a 01");
    await act(async () => Promise.resolve());
    const note = document.body.querySelector<HTMLTextAreaElement>('[role="dialog"] textarea')!;
    const setValue = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, "value")!.set!;
    await act(async () => {
      setValue.call(note, "Richiesta di prova");
      note.dispatchEvent(new Event("input", { bubbles: true }));
    });
    await click("Conferma e invia");
    expect(document.body.querySelector('[role="dialog"]')).toBeNull();

    expect(testDoubles.fetcherSubmit).toHaveBeenCalledOnce();
    expect(testDoubles.fetcherSubmit.mock.calls[0][0].get("request")).toBe("Richiesta di prova");
    expect(testDoubles.fetcherSubmit.mock.calls[0][0].get("intent")).toBe("book");
  });

  it("loads availability automatically when its selected session changes", async () => {
    const date = new Date().toISOString().slice(0, 10);
    const routeData: BookingRouteData = {
      date,
      period: "",
      offered: {
        localDate: date,
        timeZone: "Europe/Rome",
        sessions: [
          {
            periodKey: "morning",
            startUtc: `${date}T08:00:00Z`,
            endUtc: `${date}T12:00:00Z`,
          },
          {
            periodKey: "afternoon",
            startUtc: `${date}T13:00:00Z`,
            endUtc: `${date}T17:00:00Z`,
          },
        ],
      },
      config: {
        id: "default",
        creationDateTime: "",
        name: "Pepperbox",
        timeZone: "Europe/Rome",
        sessionMode: 0,
        openingDays: [0, 1, 2, 3, 4, 5, 6],
        morningStart: "09:00:00",
        morningEnd: "13:00:00",
        afternoonStart: "14:00:00",
        afternoonEnd: "18:00:00",
        hourlyMinutes: 60,
        continuousStart: null,
        continuousEnd: null,
        reservedReleaseDaysBefore: 2,
      },
      history: Promise.resolve([]),
      availability: Promise.resolve(null),
    };
    testDoubles.fetcherData = routeData;
    await act(async () =>
      root.render(
        <MemoryRouter>
          <Booking
            account={account}
            onLogout={() => {}}
            loggingOut={false}
            onPasswordChanged={() => {}}
          />
        </MemoryRouter>,
      ),
    );
    await act(async () => Promise.resolve());

    expect(testDoubles.fetcherLoad).toHaveBeenCalledWith(
      expect.stringContaining(`date=${date}&period=morning&search=1`),
    );
    const sessionSelect = container.querySelector("select")!;
    const setValue = Object.getOwnPropertyDescriptor(
      HTMLSelectElement.prototype,
      "value",
    )!.set!;
    await act(async () => {
      setValue.call(sessionSelect, "afternoon");
      sessionSelect.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await act(async () => Promise.resolve());

    expect(testDoubles.fetcherLoad).toHaveBeenCalledWith(
      expect.stringContaining(`date=${date}&period=afternoon&search=1`),
    );
  });

  it("shows a labelled pending-request badge to administrators", async () => {
    testDoubles.adminPendingBookingCount.mockResolvedValue({ count: 2 });
    await act(async () =>
      root.render(
        <MemoryRouter>
          <Shell account={{ ...account, isAdmin: true }}>
            <p>Admin</p>
          </Shell>
        </MemoryRouter>,
      ),
    );
    await act(async () => Promise.resolve());

    const requestsLink = container.querySelector<HTMLAnchorElement>(
      'a[href="/admin/bookings"]',
    );
    expect(requestsLink?.textContent).toContain("2");
    expect(requestsLink?.getAttribute("aria-label")).toContain("2 da approvare");
    expect(adminPendingBookingCount).toHaveBeenCalledOnce();
  });

  it("disables authentication fields while a request is pending", async () => {
    vi.mocked(getSession).mockRejectedValue(new Error("not authenticated"));
    let finishSubmit: () => void = () => {};
    testDoubles.login.mockReturnValue(
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

  it.each([false, true])(
    "routes a successful login to its destination (forced change: %s)",
    async (forcePasswordChange) => {
      vi.mocked(getSession).mockRejectedValue(new Error("not authenticated"));
      testDoubles.login.mockResolvedValue({
        ...account,
        forcePasswordChange,
      });
      await renderGate();

      await submitLogin();
      await act(async () => Promise.resolve());

      if (forcePasswordChange) {
        expect(container.textContent).toContain("Aggiorna la password");
      } else {
        expect(container.textContent).toContain("Prenotazioni");
      }
    },
  );

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

  it("expires a signed-in session once without restarting restoration on duplicate events", async () => {
    vi.mocked(getSession).mockResolvedValue(account);
    await renderGate();
    expect(getSession).toHaveBeenCalledOnce();

    await act(async () => {
      window.dispatchEvent(new Event("range-session-expired"));
      window.dispatchEvent(new Event("range-session-expired"));
    });

    expect(container.textContent).toContain("Bentornato");
    expect(container.textContent).not.toContain("Prenotazioni");
    expect(getSession).toHaveBeenCalledOnce();
  });

  it("revalidates on focus without replacing the mounted session view", async () => {
    vi.mocked(getSession)
      .mockResolvedValueOnce(account)
      .mockResolvedValueOnce({ ...account, firstName: "Updated" });
    await renderGate();
    expect(container.textContent).toContain("Scegli una data e una sessione");

    await act(async () => {
      window.dispatchEvent(new Event("focus"));
      window.dispatchEvent(new Event("focus"));
      await Promise.resolve();
    });

    expect(getSession).toHaveBeenCalledTimes(2);
    expect(container.textContent).toContain("Updated User");
    expect(container.textContent).not.toContain("Ripristino sessione");
  });

  it("expires the signed-in session when a focus recheck confirms a 401", async () => {
    vi.mocked(getSession)
      .mockResolvedValueOnce(account)
      .mockRejectedValueOnce(new ApiError(401, "Unauthorized"));
    await renderGate();

    await act(async () => {
      window.dispatchEvent(new Event("focus"));
      await Promise.resolve();
    });

    expect(container.textContent).toContain("Bentornato");
    expect(container.textContent).not.toContain("Scegli una data e una sessione");
    expect(getSession).toHaveBeenCalledTimes(2);
  });

  it("keeps the session view visible after a transient focus check failure and offers a retry", async () => {
    vi.mocked(getSession)
      .mockResolvedValueOnce(account)
      .mockRejectedValueOnce(new Error("network unavailable"))
      .mockResolvedValueOnce(account);
    await renderGate();

    await act(async () => window.dispatchEvent(new Event("focus")));
    expect(container.textContent).toContain("Scegli una data e una sessione");
    expect(container.textContent).toContain("Sessione non disponibile. Riprova.");
    expect(container.textContent).not.toContain("Ripristino sessione");
    await click("Riprova controllo sessione");
    expect(getSession).toHaveBeenCalledTimes(3);
    expect(container.textContent).toContain("Scegli una data e una sessione");
    expect(container.textContent).not.toContain("Sessione non disponibile");
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
    expect(container.querySelector(".mpv-appearance__trigger")).not.toBeNull();
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
