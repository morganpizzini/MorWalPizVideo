import { afterEach, describe, expect, it, vi } from "vitest";
import {
  adminUsers,
  ApiError,
  availability,
  book,
  changePassword,
  getSession,
  login,
  logout,
  saveConfig,
  sessions,
  type RangeConfig,
} from "./api";

afterEach(() => vi.unstubAllGlobals());

describe("credentialed range API", () => {
  it("loads server sessions and encodes availability keys with an abort signal", async () => {
    const fetch = vi
      .fn()
      .mockImplementation(
        async () => new Response(JSON.stringify({ sessions: [] })),
      );
    vi.stubGlobal("fetch", fetch);
    const signal = new AbortController().signal;
    await sessions("2030-01-12", signal);
    await availability("2030-01-12", "09:30", signal);
    expect(fetch.mock.calls[0][0]).toBe(
      "/api/shooting-range/sessions?date=2030-01-12",
    );
    expect(fetch.mock.calls[1][0]).toContain("period=09%3A30");
    expect(fetch.mock.calls[0][1]).toMatchObject({
      signal,
      credentials: "include",
    });
  });

  it("obtains a fresh authenticated CSRF token before booking after login", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "prelogin-token" })),
      )
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: "account" })))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "postlogin-token" })),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "authenticated-token" })),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ id: "booking" }), { status: 201 }),
      );
    vi.stubGlobal("fetch", fetch);
    await login({ username: "account", password: "test-only" });
    await book({
      bayId: "bay",
      localDate: "2030-01-12",
      periodKey: "morning",
      request: "",
      expectedStartUtc: "2030-01-12T08:00:00Z",
      expectedEndUtc: "2030-01-12T12:00:00Z",
    });
    expect(fetch.mock.calls.map((call) => call[0])).toEqual([
      "/api/shooting-range/csrf",
      "/api/shooting-range/auth/login",
      "/api/shooting-range/csrf",
      "/api/shooting-range/csrf",
      "/api/shooting-range/bookings",
    ]);
    expect(fetch.mock.calls[1][1].headers.get("X-CSRF-TOKEN")).toBe(
      "prelogin-token",
    );
    expect(fetch.mock.calls[4][1].headers.get("X-CSRF-TOKEN")).toBe(
      "authenticated-token",
    );
    expect(JSON.parse(fetch.mock.calls[4][1].body)).toMatchObject({
      expectedStartUtc: "2030-01-12T08:00:00Z",
      expectedEndUtc: "2030-01-12T12:00:00Z",
    });
  });

  it("restores cookie sessions and sends CSRF-protected logout without persisting credentials", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({ id: "account", forcePasswordChange: true }),
        ),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "logout-token" })),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetch);
    const signal = new AbortController().signal;
    expect(await getSession(signal)).toMatchObject({
      id: "account",
      forcePasswordChange: true,
    });
    await logout();
    expect(fetch.mock.calls.map((call) => call[0])).toEqual([
      "/api/shooting-range/auth/session",
      "/api/shooting-range/csrf",
      "/api/shooting-range/auth/logout",
    ]);
    expect(fetch.mock.calls[0][1]).toMatchObject({
      signal,
      credentials: "include",
    });
    expect(fetch.mock.calls[2][1].headers.get("X-CSRF-TOKEN")).toBe(
      "logout-token",
    );
  });

  it("refreshes CSRF after self password change renews the session", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "old-identity" })),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ token: "renewed-identity" })),
      );
    vi.stubGlobal("fetch", fetch);
    await changePassword({
      currentPassword: "test-password",
      newPassword: "replacement-password",
    });
    expect(fetch.mock.calls.map((call) => call[0])).toEqual([
      "/api/shooting-range/csrf",
      "/api/shooting-range/auth/password",
      "/api/shooting-range/csrf",
    ]);
  });

  it.each([401, 429])(
    "preserves session or throttling failure %s",
    async (status) => {
      vi.stubGlobal(
        "fetch",
        vi
          .fn()
          .mockResolvedValue(
            new Response(JSON.stringify({ message: "Retry sign-in" }), {
              status,
            }),
          ),
      );
      await expect(getSession()).rejects.toMatchObject({ status });
    },
  );

  it("sends numeric config mode unchanged with nullable additive times", async () => {
    const fetch = vi
      .fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: "token" })))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: "default" })));
    vi.stubGlobal("fetch", fetch);
    const config = {
      sessionMode: 2,
      hourlyMinutes: 60,
      continuousStart: "09:30:00",
      continuousEnd: "13:00:00",
    } as RangeConfig;
    await saveConfig(config);
    expect(JSON.parse(fetch.mock.calls[1][1].body)).toMatchObject(config);
    expect(fetch.mock.calls[1][1]).toMatchObject({
      method: "PUT",
      credentials: "include",
    });
  });

  it.each([400, 403, 409, 503])(
    "preserves actionable status %s without treating failure as success",
    async (status) => {
      vi.stubGlobal(
        "fetch",
        vi
          .fn()
          .mockResolvedValue(
            new Response(JSON.stringify({ message: "Unavailable" }), {
              status,
            }),
          ),
      );
      await expect(adminUsers()).rejects.toMatchObject({
        status,
        message: "Unavailable",
      });
      await expect(adminUsers()).rejects.toBeInstanceOf(ApiError);
    },
  );
});
