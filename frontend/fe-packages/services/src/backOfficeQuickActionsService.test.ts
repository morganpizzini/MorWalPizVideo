import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getDashboardQuickActions,
  resetDashboardQuickActions,
  saveDashboardQuickActions,
} from "./backOfficeQuickActionsService";
import {
  accountApiService,
  adminApiService,
  publicApiService,
  resolveChannelBootstrap,
  startChannelBootstrap,
} from "./apiTransport";

afterEach(() => {
  vi.unstubAllGlobals();
  adminApiService.setSelectedChannelId(null);
  adminApiService.resetCsrfToken();
});

describe("personal quick-action transport", () => {
  it("uses the versioned scoped endpoint and preserves literal strings", async () => {
    const fetchMock = vi.fn().mockImplementation((_url, options) => {
      if (String(_url).endsWith("/api/auth/csrf"))
        return Promise.resolve(Response.json({ token: "csrf" }));
      return Promise.resolve(
        Response.json({ actions: [], channelId: "channel" }),
      );
    });
    vi.stubGlobal("fetch", fetchMock);
    resolveChannelBootstrap(startChannelBootstrap(), "channel");
    const request = {
      actions: [
        {
          id: "one",
          kind: "custom" as const,
          label: "  <自由>  ",
          url: "missing ?x=%2f#Here  ",
        },
      ],
    };
    await saveDashboardQuickActions(request);
    const [url, options] = fetchMock.mock.calls.find(
      ([, init]) => init.method === "PUT",
    )!;
    expect(String(url)).toMatch(/\/api\/v1\/dashboard\/quick-actions$/);
    expect(JSON.parse(options.body)).toEqual(request);
    expect(new Headers(options.headers).get("X-Channel-Id")).toBe("channel");
    expect(new Headers(options.headers).get("X-CSRF-TOKEN")).toBe("csrf");
    expect(options.credentials).toBe("include");
  });

  it("uses GET and DELETE and never converts write failures to success", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockImplementation((_url, options) =>
          Promise.resolve(
            Response.json(
              options.method === "DELETE"
                ? { status: 500, detail: "Storage unavailable" }
                : { channelId: "channel" },
              { status: options.method === "DELETE" ? 500 : 200 },
            ),
          ),
        ),
    );
    resolveChannelBootstrap(startChannelBootstrap(), "channel");
    await expect(getDashboardQuickActions()).resolves.toMatchObject({
      channelId: "channel",
    });
    await expect(resetDashboardQuickActions()).rejects.toThrow();
  });

  it("keeps the new prefix isolated in public and account transports", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({}));
    vi.stubGlobal("fetch", fetchMock);
    adminApiService.setSelectedChannelId("private-channel");
    await publicApiService.get("/api/v1/dashboard/quick-actions");
    fetchMock.mockResolvedValue(Response.json({}));
    await accountApiService.get("/api/v1/dashboard/quick-actions");
    for (const [, options] of fetchMock.mock.calls)
      expect(new Headers(options.headers).has("X-Channel-Id")).toBe(false);
  });
});
