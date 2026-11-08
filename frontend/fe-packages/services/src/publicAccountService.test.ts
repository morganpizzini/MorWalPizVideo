import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getPublicAccountErrorMessage,
  getPublicAccountSession,
  loginPublicAccount,
  logoutPublicAccount,
} from "./publicAccountService";
import { ApiResponseError, accountApiService } from "./apiTransport";

afterEach(() => {
  vi.unstubAllGlobals();
  accountApiService.resetCsrfToken();
});

describe("public account transport", () => {
  it("rejects an error envelope returned by the session endpoint", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          Response.json({ errors: ["not authenticated"] }, { status: 401 }),
        ),
    );

    await expect(getPublicAccountSession()).rejects.toMatchObject({
      status: 401,
    });
  });

  it("accepts an anonymous session but rejects malformed session data", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(Response.json({ authenticated: false }))
        .mockResolvedValueOnce(Response.json({ account: {} })),
    );

    await expect(getPublicAccountSession()).resolves.toEqual({
      authenticated: false,
    });
    await expect(getPublicAccountSession()).rejects.toThrow(
      "The account session response was invalid",
    );
  });

  it("requests session state without using a cached anonymous response", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      Response.json({ authenticated: false }),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(getPublicAccountSession()).resolves.toEqual({
      authenticated: false,
    });

    expect(fetchMock.mock.calls[0][1]?.cache).toBe("no-store");
  });

  it("keeps account errors safe and status-aware for an empty session response", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 429 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(getPublicAccountSession()).rejects.toMatchObject({
      status: 429,
    });
    expect(
      getPublicAccountErrorMessage(new ApiResponseError({ status: 429 })),
    ).toContain("Troppe richieste");
    expect(getPublicAccountErrorMessage(new Error("network"))).toContain(
      "Riprova",
    );
  });

  it("uses the freshly fetched CSRF token without relying on the browser cache", async () => {
    const csrfToken = "fresh-account-csrf-token";
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: csrfToken })
          : Response.json({ title: "Invalid account credentials." }, { status: 401 }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      loginPublicAccount({ identifier: "unknown", password: "wrong-password" }),
    ).rejects.toMatchObject({ status: 401 });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][1]?.cache).toBe("no-store");
    const loginRequest = fetchMock.mock.calls[1][1];
    expect(loginRequest?.credentials).toBe("include");
    expect(new Headers(loginRequest?.headers).get("X-CSRF-TOKEN")).toBe(csrfToken);
    expect(JSON.parse(String(loginRequest?.body))).toEqual({
      identifier: "unknown",
      password: "wrong-password",
    });
  });

  it("continues to acquire CSRF for the authenticated logout endpoint", async () => {
    const csrfToken = "logout-account-csrf-token";
    const fetchMock = vi.fn((input: RequestInfo | URL) =>
      Promise.resolve(
        String(input).endsWith("/csrf")
          ? Response.json({ token: csrfToken })
          : new Response(null, { status: 204 }),
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    await logoutPublicAccount();

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][1]?.cache).toBe("no-store");
    expect(new Headers(fetchMock.mock.calls[1][1]?.headers).get("X-CSRF-TOKEN")).toBe(
      csrfToken,
    );
  });
});
