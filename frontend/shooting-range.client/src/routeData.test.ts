import { afterEach, describe, expect, it, vi } from "vitest";
import {
  ApiError,
  adminConfig,
  book,
  getSession,
  myBookings,
  sessions,
  type Account,
  type Sessions,
} from "./api";
import type { ActionFunctionArgs, LoaderFunctionArgs } from "react-router";
import {
  adminAction,
  adminLoader,
  bookingAction,
  bookingLoader,
} from "./routeData";

vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  adminConfig: vi.fn(),
  book: vi.fn(),
  getSession: vi.fn(),
  messages: vi.fn(),
  myBookings: vi.fn(),
  openMessage: vi.fn(),
  sessions: vi.fn(),
}));

const account: Account = {
  id: "approved",
  username: "approved",
  firstName: "Range",
  lastName: "Member",
  status: 1,
  isAdmin: false,
  forcePasswordChange: false,
};

const offered: Sessions = {
  localDate: "2030-01-12",
  timeZone: "Europe/Rome",
  sessions: [{
    periodKey: "09:00",
    startUtc: "2030-01-12T08:00:00Z",
    endUtc: "2030-01-12T09:00:00Z",
  }],
};

function loaderArgs(url: string): LoaderFunctionArgs {
  return {
    request: new Request(url),
    params: {},
    context: {},
  } as LoaderFunctionArgs;
}

function actionArgs(path: string, values: Record<string, string>): ActionFunctionArgs {
  const formData = new FormData();
  for (const [key, value] of Object.entries(values)) formData.set(key, value);
  return {
    request: new Request(`https://range.test${path}`, {
      method: "POST",
      body: formData,
    }),
    params: {},
    context: {},
  } as ActionFunctionArgs;
}

afterEach(() => {
  vi.unstubAllGlobals();
  vi.resetAllMocks();
});

describe("Range route data security and mutations", () => {
  it("does not make session or domain requests during SSR loaders", async () => {
    vi.stubGlobal("window", undefined);

    await expect(bookingLoader(loaderArgs("https://range.test/"))).resolves.toBeNull();
    expect(getSession).not.toHaveBeenCalled();
    expect(sessions).not.toHaveBeenCalled();
    expect(myBookings).not.toHaveBeenCalled();
  });

  it("checks admin role before starting admin reads", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);

    const result = await adminLoader(loaderArgs("https://range.test/admin"));

    expect(result).toMatchObject({
      sessionError: { error: true, status: 403 },
    });
    expect(adminConfig).not.toHaveBeenCalled();
  });

  it("keeps loader data deferred after validating the browser session", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(myBookings).mockResolvedValue([]);

    const result = await bookingLoader(
      loaderArgs("https://range.test/?date=2030-01-12"),
    );

    expect(result).toMatchObject({ date: "2030-01-12" });
    if (!result || !("offered" in result)) throw new Error("Expected booking loader data.");
    expect(result.offered).toBeInstanceOf(Promise);
    await expect(result.offered).resolves.toEqual(offered);
    await expect(result.history).resolves.toEqual([]);
  });

  it("rejects stale booking boundaries before issuing the mutation", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(book).mockResolvedValue({
      id: "booking",
      creationDateTime: "2030-01-01T00:00:00Z",
      userId: account.id,
      bayId: "bay-1",
      localDate: "2030-01-12",
      periodKey: "09:00",
      startUtc: offered.sessions[0].startUtc,
      endUtc: offered.sessions[0].endUtc,
      request: "",
      status: 0,
    });

    const result = await bookingAction(actionArgs("/", {
      intent: "book",
      bayId: "bay-1",
      localDate: "2030-01-12",
      periodKey: "09:00",
      expectedStartUtc: "2030-01-12T07:00:00Z",
      expectedEndUtc: offered.sessions[0].endUtc,
    }));

    expect(result).toMatchObject({ success: false });
    expect(result.message).toContain("Gli orari sono cambiati");
    expect(book).not.toHaveBeenCalled();
  });

  it("rechecks administrator authorization before admin mutations", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);

    const result = await adminAction(actionArgs("/admin", {
      intent: "approve-user",
      id: "pending-user",
    }));

    expect(result).toMatchObject({ success: false });
    expect(result.status).toBe(403);
  });

  it("expires the browser session after a protected action receives 401", async () => {
    const browser = new EventTarget();
    vi.stubGlobal("window", browser);
    vi.mocked(getSession).mockRejectedValue(new ApiError(401, "Unauthorized"));
    const onExpired = vi.fn();
    browser.addEventListener("range-session-expired", onExpired);

    const result = await adminAction(actionArgs("/admin", {
      intent: "approve-user",
      id: "pending-user",
    }));

    expect(result.status).toBe(401);
    expect(onExpired).toHaveBeenCalledOnce();
  });
});
