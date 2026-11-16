import { afterEach, describe, expect, it, vi } from "vitest";
import { createApiClient } from "./apiTransport";

describe("account API unauthorized handling", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("invokes the unauthorized handler for expired account requests", async () => {
    const unauthorized = vi.fn();
    const client = createApiClient({ mode: "account", baseUrl: "https://account.test" });
    client.setUnauthorizedHandler(unauthorized);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      Response.json({ detail: "Authentication failed" }, { status: 401 }),
    ));

    await client.get("/api/v1/workshop/dashboard");

    expect(unauthorized).toHaveBeenCalledOnce();
  });

  it("does not invoke the handler for account authentication requests", async () => {
    const unauthorized = vi.fn();
    const client = createApiClient({ mode: "account", baseUrl: "https://account.test" });
    client.setUnauthorizedHandler(unauthorized);
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      Response.json({ detail: "Authentication failed" }, { status: 401 }),
    ));

    await client.post("/api/v1/account/sessions", {});

    expect(unauthorized).not.toHaveBeenCalled();
  });

  it("refreshes the CSRF token and retries a rejected account mutation once", async () => {
    const client = createApiClient({ mode: "account", baseUrl: "https://account.test" });
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith("/csrf")) {
        return Promise.resolve(Response.json(
          { token: fetchMock.mock.calls.length === 1 ? "csrf-1" : "csrf-2" },
        ));
      }
      if (fetchMock.mock.calls.length === 2) {
        return Promise.resolve(Response.json(
          { code: "csrf_invalid", title: "The CSRF token is invalid." },
          { status: 403 },
        ));
      }
      return Promise.resolve(Response.json({ saved: true }));
    });
    vi.stubGlobal("fetch", fetchMock);

    const response = await client.post("/api/v1/workshop/components/install", {
      definitionId: "definition-1",
    });

    expect(response).toEqual({ saved: true });
    expect(fetchMock).toHaveBeenCalledTimes(4);
    expect(new Headers(fetchMock.mock.calls[1][1]?.headers).get("X-CSRF-TOKEN")).toBe("csrf-1");
    expect(new Headers(fetchMock.mock.calls[3][1]?.headers).get("X-CSRF-TOKEN")).toBe("csrf-2");
  });
});
