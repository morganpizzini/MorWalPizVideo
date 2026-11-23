import { afterEach, describe, expect, it, vi } from "vitest";
import {
  acceptFriendRequest,
  ApiError,
  adminBays,
  adminBookings,
  adminBookingHistory,
  adminBookingPolicy,
  adminBookingReport,
  adminCancelBooking,
  adminConfig,
  adminUsers,
  approveUser,
  availability,
  book,
  cancelBooking,
  changePassword,
  createMechanismType,
  createAdminBooking,
  decideBooking,
  decideMessageStatus,
  getConfig,
  getSession,
  friends,
  friendRequests,
  login,
  messages,
  myBookings,
  sessions,
  sendFriendRequest,
  cancelFriendRequest,
  declineFriendRequest,
  removeFriend,
  saveConfig,
  saveBookingPolicy,
  setBookingAttendance,
  type Account,
  type RangeConfig,
  type Sessions,
} from "./api";
import type { ActionFunctionArgs, LoaderFunctionArgs } from "react-router";
import {
  adminAction,
  adminBookingsLoader,
  adminLoader,
  adminReportLoader,
  bookingAction,
  bookingLoader,
  friendsAction,
  friendsLoader,
  messagesAction,
  messagesLoader,
} from "./routeData";

vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  acceptFriendRequest: vi.fn(),
  cancelFriendRequest: vi.fn(),
  declineFriendRequest: vi.fn(),
  adminConfig: vi.fn(),
  adminBookingPolicy: vi.fn(),
  adminBookingHistory: vi.fn(),
  adminBookingReport: vi.fn(),
  adminCancelBooking: vi.fn(),
  adminBays: vi.fn(),
  adminBookings: vi.fn(),
  adminUsers: vi.fn(),
  approveUser: vi.fn(),
  availability: vi.fn(),
  book: vi.fn(),
  cancelBooking: vi.fn(),
  changePassword: vi.fn(),
  createMechanismType: vi.fn(),
  createAdminBooking: vi.fn(),
  decideBooking: vi.fn(),
  decideMessageStatus: vi.fn(),
  getConfig: vi.fn(),
  getSession: vi.fn(),
  friends: vi.fn(),
  friendRequests: vi.fn(),
  login: vi.fn(),
  messages: vi.fn(),
  myBookings: vi.fn(),
  openMessage: vi.fn(),
  sessions: vi.fn(),
  sendFriendRequest: vi.fn(),
  removeFriend: vi.fn(),
  saveConfig: vi.fn(),
  saveBookingPolicy: vi.fn(),
  setBookingAttendance: vi.fn(),
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
const config: RangeConfig = {
  id: "default",
  creationDateTime: "",
  name: "Pepperbox",
  timeZone: "Europe/Rome",
  sessionMode: 0,
  openingDays: [0, 6],
  morningStart: "09:00:00",
  morningEnd: "13:00:00",
  afternoonStart: "14:00:00",
  afternoonEnd: "18:00:00",
  hourlyMinutes: 60,
  continuousStart: null,
  continuousEnd: null,
  reservedReleaseDaysBefore: 2,
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

  it("loads the dedicated reservation-decision page from the current admin session", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(adminConfig).mockResolvedValue(config);
    vi.mocked(adminBookingPolicy).mockResolvedValue({
      revision: 0,
      minimumCancellationDays: 0,
      noShowRules: [],
    });
    vi.mocked(adminUsers).mockResolvedValue([]);
    vi.mocked(adminBays).mockResolvedValue([]);
    vi.mocked(adminBookings).mockResolvedValue([]);
    vi.mocked(adminBookingHistory).mockResolvedValue({
      items: [],
      page: 0,
      pageSize: 25,
      totalCount: 0,
    });
    vi.mocked(sessions).mockResolvedValue(offered);

    await expect(adminBookingsLoader(
      loaderArgs("https://range.test/admin/bookings?historyDate=2030-01-01"),
    )).resolves.toMatchObject({
      config,
      users: [],
      bays: [],
      bookings: [],
      historyDate: "2030-01-01",
      history: { items: [], page: 0, pageSize: 25, totalCount: 0 },
    });
  });

  it("does not fetch an administrator report until a user is selected", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(adminUsers).mockResolvedValue([]);
    vi.mocked(adminConfig).mockResolvedValue(config);

    await expect(adminReportLoader(loaderArgs("https://range.test/admin/reports")))
      .resolves.toMatchObject({ users: [], report: null, userId: "" });
    expect(adminBookingReport).not.toHaveBeenCalled();
  });

  it("loads a bounded user report with the selected attendance filter", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(adminUsers).mockResolvedValue([]);
    vi.mocked(adminConfig).mockResolvedValue(config);
    vi.mocked(adminBookingReport).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 30,
    });

    const result = await adminReportLoader(loaderArgs(
      "https://range.test/admin/reports?userId=user-1&attendance=no-show&page=1",
    ));

    expect(result).toMatchObject({
      userId: "user-1",
      attendance: "no-show",
      report: { page: 1, totalCount: 30 },
    });
    expect(adminBookingReport).toHaveBeenCalledWith(
      "user-1",
      "no-show",
      1,
      25,
      expect.any(AbortSignal),
    );
  });

  it("preserves stale-policy conflicts as visible administrator feedback", async () => {
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(saveBookingPolicy).mockRejectedValue(new ApiError(409, "Settings changed. Refresh."));

    const result = await adminAction(actionArgs("/admin", {
      intent: "save-booking-policy",
      payload: JSON.stringify({
        revision: 0,
        minimumCancellationDays: 2,
        noShowRules: [],
      }),
    }));

    expect(result).toMatchObject({
      success: false,
      status: 409,
      message: "Settings changed. Refresh.",
    });
  });

  it("loads message threads for members and sender identities only for admins", async () => {
    vi.stubGlobal("window", {});
    const thread = {
      id: "thread-1",
      creationDateTime: "2030-01-01T10:00:00Z",
      userId: account.id,
      subject: "Help",
      status: 0,
      messages: [],
    };
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(messages).mockResolvedValue([thread]);
    vi.mocked(adminUsers).mockResolvedValue([]);

    const memberResult = await messagesLoader(loaderArgs("https://range.test/messages"));
    if (!memberResult || !("items" in memberResult)) throw new Error("Expected message data.");
    await expect(memberResult.items).resolves.toEqual({ threads: [thread], users: [] });
    expect(adminUsers).not.toHaveBeenCalled();

    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(adminUsers).mockResolvedValue([{
      ...account,
      creationDateTime: "2030-01-01T00:00:00Z",
    }]);
    const adminResult = await messagesLoader(loaderArgs("https://range.test/messages"));
    if (!adminResult || !("items" in adminResult)) throw new Error("Expected admin message data.");
    await expect(adminResult.items).resolves.toMatchObject({
      threads: [thread],
      users: [{ id: account.id, username: account.username }],
    });
    expect(adminUsers).toHaveBeenCalledOnce();
  });

  it("requires an administrator before accepting a message status decision", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);

    const denied = await messagesAction(actionArgs("/messages", {
      intent: "decide-message-status",
      payload: JSON.stringify({ id: "thread-1", status: 2 }),
    }));
    expect(denied).toMatchObject({ success: false, status: 403 });
    expect(decideMessageStatus).not.toHaveBeenCalled();

    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(decideMessageStatus).mockResolvedValue({
      id: "thread-1",
      creationDateTime: "2030-01-01T10:00:00Z",
      userId: account.id,
      subject: "Help",
      status: 2,
      messages: [],
    });
    const updated = await messagesAction(actionArgs("/messages", {
      intent: "decide-message-status",
      payload: JSON.stringify({ id: "thread-1", status: 2 }),
    }));
    expect(updated).toMatchObject({
      intent: "decide-message-status",
      success: true,
      message: "Stato del messaggio aggiornato: Completata.",
    });
    expect(decideMessageStatus).toHaveBeenCalledWith("thread-1", 2);

    const invalid = await messagesAction(actionArgs("/messages", {
      intent: "decide-message-status",
      payload: JSON.stringify({ id: "thread-1", status: 0 }),
    }));
    expect(invalid).toMatchObject({ success: false });
    expect(decideMessageStatus).toHaveBeenCalledOnce();
  });

  it("loads sessions and opening weekdays after validating the browser session", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(getConfig).mockResolvedValue(config);
    vi.mocked(myBookings).mockResolvedValue([]);

    const result = await bookingLoader(
      loaderArgs("https://range.test/?date=2030-01-12"),
    );

    expect(result).toMatchObject({
      date: "2030-01-12",
      offered,
      config,
    });
    if (
      !result ||
      !("offered" in result) ||
      !("config" in result) ||
      result.config === undefined
    ) {
      throw new Error("Expected booking loader data.");
    }
    expect(result.offered).toEqual(offered);
    expect(result.config.openingDays).toEqual([0, 6]);
    await expect(result.history).resolves.toEqual([]);
  });

  it("surfaces field configuration failures instead of enabling an unfiltered calendar", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(getConfig).mockRejectedValue(new Error("Configuration unavailable"));

    await expect(
      bookingLoader(loaderArgs("https://range.test/?date=2030-01-12")),
    ).resolves.toMatchObject({
      sessionError: { error: true, message: "Configuration unavailable" },
    });
    expect(myBookings).not.toHaveBeenCalled();
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
      request: "",
      expectedStartUtc: "2030-01-12T07:00:00Z",
      expectedEndUtc: offered.sessions[0].endUtc,
    }));

    expect(result).toMatchObject({ success: false });
    expect(result.message).toContain("Gli orari sono cambiati");
    expect(book).not.toHaveBeenCalled();
  });

  it("keeps the confirmed member note and rejects notes over the existing limit", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(book).mockResolvedValue({
      id: "booking",
      creationDateTime: "2030-01-01T00:00:00Z",
      userId: account.id,
      bayId: "bay-1",
      localDate: "2030-01-12",
      periodKey: offered.sessions[0].periodKey,
      startUtc: offered.sessions[0].startUtc,
      endUtc: offered.sessions[0].endUtc,
      request: "Setup note",
      status: 0,
    });

    const confirmed = await bookingAction(actionArgs("/", {
      intent: "book",
      bayId: "bay-1",
      localDate: "2030-01-12",
      periodKey: offered.sessions[0].periodKey,
      expectedStartUtc: offered.sessions[0].startUtc,
      expectedEndUtc: offered.sessions[0].endUtc,
      request: "Setup note",
    }));
    expect(confirmed).toMatchObject({ success: true });
    expect(book).toHaveBeenCalledWith(expect.objectContaining({ request: "Setup note" }));

    vi.mocked(book).mockClear();
    const tooLong = await bookingAction(actionArgs("/", {
      intent: "book",
      bayId: "bay-1",
      localDate: "2030-01-12",
      periodKey: offered.sessions[0].periodKey,
      expectedStartUtc: offered.sessions[0].startUtc,
      expectedEndUtc: offered.sessions[0].endUtc,
      request: "x".repeat(1001),
    }));
    expect(tooLong).toMatchObject({ success: false, message: "La nota non può superare 1000 caratteri." });
    expect(book).not.toHaveBeenCalled();
  });

  it("surfaces stale booking conflicts after validating the current session", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(book).mockRejectedValue(new ApiError(409, "Conflict"));

    const result = await bookingAction(actionArgs("/", {
      intent: "book",
      bayId: "bay-1",
      localDate: offered.localDate,
      periodKey: offered.sessions[0].periodKey,
      expectedStartUtc: offered.sessions[0].startUtc,
      expectedEndUtc: offered.sessions[0].endUtc,
      request: "Keep this note",
    }));

    expect(result).toMatchObject({
      success: false,
      status: 409,
      message: "La postazione non è più disponibile. Seleziona un altro turno.",
    });
  });

  it("loads availability only for a currently offered selected session", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(getConfig).mockResolvedValue(config);
    vi.mocked(myBookings).mockResolvedValue([]);
    vi.mocked(availability).mockResolvedValue({
      localDate: offered.localDate,
      periodKey: offered.sessions[0].periodKey,
      startUtc: offered.sessions[0].startUtc,
      endUtc: offered.sessions[0].endUtc,
      bays: [],
    });

    const result = await bookingLoader(loaderArgs(
      "https://range.test/?date=2030-01-12&period=09%3A00&search=1&region=availability",
    ));

    if (!result || !("availability" in result)) throw new Error("Expected booking loader data.");
    await expect(result.availability).resolves.toMatchObject({
      periodKey: "09:00",
      bays: [],
    });
    expect(availability).toHaveBeenCalledWith(
      "2030-01-12",
      "09:00",
      expect.any(AbortSignal),
    );
  });

  it("cancels a reservation through the protected route action", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(cancelBooking).mockResolvedValue(undefined);

    const result = await bookingAction(actionArgs("/", {
      intent: "cancel-booking",
      id: "booking-1",
    }));

    expect(result).toMatchObject({ intent: "cancel-booking", success: true });
    expect(cancelBooking).toHaveBeenCalledWith("booking-1");
  });

  it("enforces seven-character passwords before password change", async () => {
    vi.stubGlobal("window", {});
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

  it("submits administrator approval and decision payloads to their matching APIs", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(approveUser).mockResolvedValue(undefined);
    vi.mocked(decideBooking).mockResolvedValue(undefined);

    const approvedUser = await adminAction(actionArgs("/admin", {
      intent: "approve-user",
      payload: JSON.stringify({ id: "pending-user" }),
    }));
    const decidedBooking = await adminAction(actionArgs("/admin/bookings", {
      intent: "decide-booking",
      payload: JSON.stringify({ id: "booking-1", approved: true }),
    }));

    expect(approvedUser.success).toBe(true);
    expect(decidedBooking.success).toBe(true);
    expect(approveUser).toHaveBeenCalledWith("pending-user");
    expect(decideBooking).toHaveBeenCalledWith("booking-1", true);
  });

  it("accepts Mixed settings with 60- or 120-minute increments", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(saveConfig).mockResolvedValue({ ...config, sessionMode: 3, hourlyMinutes: 120 });

    const result = await adminAction(actionArgs("/admin", {
      intent: "save-config",
      payload: JSON.stringify({ ...config, sessionMode: 3, hourlyMinutes: 120 }),
    }));

    expect(result).toMatchObject({ success: true });
    expect(saveConfig).toHaveBeenCalledWith(expect.objectContaining({
      sessionMode: 3,
      hourlyMinutes: 120,
    }));
  });

  it("creates an auto-approved admin booking only for a currently offered interval", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue({ ...account, isAdmin: true });
    vi.mocked(sessions).mockResolvedValue(offered);
    vi.mocked(createAdminBooking).mockResolvedValue(undefined);

    const payload = {
      userId: "member-1",
      bayId: "bay-1",
      localDate: offered.localDate,
      periodKey: offered.sessions[0].periodKey,
      request: "Setup",
      expectedStartUtc: offered.sessions[0].startUtc,
      expectedEndUtc: offered.sessions[0].endUtc,
    };
    const result = await adminAction(actionArgs("/admin/bookings", {
      intent: "create-admin-booking",
      payload: JSON.stringify(payload),
    }));

    expect(result).toMatchObject({ success: true, message: "Prenotazione creata e approvata." });
    expect(createAdminBooking).toHaveBeenCalledWith(payload);
  });

  it("sends an invitation without claiming immediate friendship and handles request decisions", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sendFriendRequest).mockResolvedValue(undefined);
    vi.mocked(acceptFriendRequest).mockResolvedValue(undefined);
    vi.mocked(declineFriendRequest).mockResolvedValue(undefined);
    vi.mocked(cancelFriendRequest).mockResolvedValue(undefined);
    vi.mocked(removeFriend).mockResolvedValue(undefined);

    const sent = await friendsAction(actionArgs("/friends", {
      intent: "send-friend-request",
      username: " exact-name ",
    }));
    const accepted = await friendsAction(actionArgs("/friends", {
      intent: "accept-friend-request",
      requestId: "incoming-1",
    }));
    const declined = await friendsAction(actionArgs("/friends", {
      intent: "decline-friend-request",
      requestId: "incoming-2",
    }));
    const cancelled = await friendsAction(actionArgs("/friends", {
      intent: "cancel-friend-request",
      requestId: "outgoing-1",
    }));
    const removed = await friendsAction(actionArgs("/friends", {
      intent: "remove-friend",
      userId: "friend-1",
    }));

    expect(sent).toMatchObject({
      success: true,
      message: expect.stringContaining("solo dopo l'accettazione"),
    });
    expect(accepted).toMatchObject({ success: true, message: "Richiesta accettata. Ora siete amici." });
    expect(declined).toMatchObject({ success: true, message: "Richiesta rifiutata." });
    expect(cancelled).toMatchObject({ success: true, message: "Richiesta annullata." });
    expect(removed).toMatchObject({ success: true });
    expect(sendFriendRequest).toHaveBeenCalledWith("exact-name");
    expect(acceptFriendRequest).toHaveBeenCalledWith("incoming-1");
    expect(declineFriendRequest).toHaveBeenCalledWith("incoming-2");
    expect(cancelFriendRequest).toHaveBeenCalledWith("outgoing-1");
    expect(removeFriend).toHaveBeenCalledWith("friend-1");
  });

  it("loads accepted friends and request directions separately for the current member", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    const friend = { id: "friend-1", username: "friend", firstName: "A", lastName: "B" };
    const requests = { incoming: [], outgoing: [] };
    vi.mocked(friends).mockResolvedValue([friend]);
    vi.mocked(friendRequests).mockResolvedValue(requests);

    await expect(friendsLoader(loaderArgs("https://range.test/friends"))).resolves.toEqual({
      friends: [friend],
      requests,
    });
    expect(friends).toHaveBeenCalledOnce();
    expect(friendRequests).toHaveBeenCalledOnce();
  });

  it("returns an invitation API error without reporting success", async () => {
    vi.stubGlobal("window", {});
    vi.mocked(getSession).mockResolvedValue(account);
    vi.mocked(sendFriendRequest).mockRejectedValue(new ApiError(409, "Richiesta già presente."));

    await expect(friendsAction(actionArgs("/friends", {
      intent: "send-friend-request",
      username: "exact-name",
    }))).resolves.toMatchObject({
      success: false,
      status: 409,
      message: "Richiesta già presente.",
    });
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
