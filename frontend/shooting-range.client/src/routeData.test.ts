import { afterEach, describe, expect, it, vi } from "vitest";
import {
  ApiError,
  adminConfig,
  book,
  changePassword,
  createMechanismType,
  getSession,
  login,
  myBookings,
  register,
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
  changePassword: vi.fn(),
  createMechanismType: vi.fn(),
  getSession: vi.fn(),
  login: vi.fn(),
  messages: vi.fn(),
  myBookings: vi.fn(),
  openMessage: vi.fn(),
  register: vi.fn(),
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
  it("does not treat an anonymous session-check 401 as an expired signed-in session", async () => {
    const browser = new EventTarget();
    vi.stubGlobal("window", browser);
    vi.mocked(getSession).mockRejectedValue(new ApiError(401, "Unauthorized"));
    const onExpired = vi.fn();
    browser.addEventListener("range-session-expired", onExpired);

    await bookingLoader(loaderArgs("https://range.test/"));

    expect(onExpired).not.toHaveBeenCalled();
  });

  it("shows a generic login error for invalid credentials without expiring the session", async () => {
    const browser = new EventTarget();
    vi.stubGlobal("window", browser);
    vi.mocked(login).mockRejectedValue(new ApiError(401, "Credentials invalid."));
    const onExpired = vi.fn();
    browser.addEventListener("range-session-expired", onExpired);

    const result = await bookingAction(actionArgs("/", {
      intent: "login",
      username: "unknown",
      password: "wrong-password",
    }));

    expect(result).toMatchObject({
      success: false,
      status: 401,
      message: "Username o password non validi, oppure account non approvato.",
    });
    expect(onExpired).not.toHaveBeenCalled();
  });

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

  it("enforces seven-character passwords before registration and password change", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(register).mockResolvedValue(account);

    const shortRegistration = await bookingAction(actionArgs("/", {
      intent: "register",
      username: "new-user",
      password: "123456",
      firstName: "New",
      lastName: "User",
    }));
    expect(shortRegistration).toMatchObject({ success: false });
    expect(register).not.toHaveBeenCalled();

    const acceptedRegistration = await bookingAction(actionArgs("/", {
      intent: "register",
      username: "new-user",
      password: "1234567",
      firstName: "New",
      lastName: "User",
    }));
    expect(acceptedRegistration).toMatchObject({ success: true });
    expect(register).toHaveBeenCalledOnce();

    vi.mocked(getSession).mockResolvedValue(account);
    const shortChange = await bookingAction(actionArgs("/", {
      intent: "password-change",
      currentPassword: "current",
      newPassword: "123456",
    }));
    expect(shortChange).toMatchObject({ success: false });
    expect(changePassword).not.toHaveBeenCalled();

    vi.mocked(changePassword).mockResolvedValue(undefined);
    const acceptedChange = await bookingAction(actionArgs("/", {
      intent: "password-change",
      currentPassword: "current",
      newPassword: "1234567",
    }));
    expect(acceptedChange).toMatchObject({ success: true });
    expect(changePassword).toHaveBeenCalledOnce();
  });

  it("keeps a valid forced-change session when the current password is incorrect", async () => {
    const browser = new EventTarget();
    vi.stubGlobal("window", browser);
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(changePassword).mockRejectedValue(new ApiError(401, "Unauthorized"));
    const onExpired = vi.fn();
    browser.addEventListener("range-session-expired", onExpired);

    const result = await bookingAction(actionArgs("/", {
      intent: "password-change",
      currentPassword: "incorrect",
      newPassword: "replacement",
    }));

    expect(result).toMatchObject({
      success: false,
      status: 401,
      message: "La password attuale non è corretta.",
    });
    expect(getSession).toHaveBeenCalledTimes(2);
    expect(onExpired).not.toHaveBeenCalled();
  });

  it("expires the session when a password-change 401 is confirmed by session inspection", async () => {
    const browser = new EventTarget();
    vi.stubGlobal("window", browser);
    vi.mocked(getSession)
      .mockResolvedValueOnce(account)
      .mockRejectedValueOnce(new ApiError(401, "Unauthorized"));
    vi.mocked(changePassword).mockRejectedValue(new ApiError(401, "Unauthorized"));
    const onExpired = vi.fn();
    browser.addEventListener("range-session-expired", onExpired);

    const result = await bookingAction(actionArgs("/", {
      intent: "password-change",
      currentPassword: "password",
      newPassword: "replacement",
    }));

    expect(result).toMatchObject({
      success: false,
      status: 401,
      message: "Sessione scaduta. Accedi di nuovo.",
    });
    expect(onExpired).toHaveBeenCalledOnce();
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

  it("creates mechanism types only after the current administrator is verified", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(createMechanismType).mockResolvedValue({ id: "type", name: "Pepper popper" });

    const result = await adminAction(actionArgs("/admin", {
      intent: "create-mechanism-type",
      payload: JSON.stringify({ name: "Pepper popper" }),
    }));

    expect(result).toMatchObject({ intent: "create-mechanism-type", success: true });
    expect(createMechanismType).toHaveBeenCalledWith("Pepper popper");
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
