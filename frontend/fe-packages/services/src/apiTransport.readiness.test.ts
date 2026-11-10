import { afterEach, describe, expect, it, vi } from "vitest";
import {
  adminApiService,
  get,
  setSelectedChannelId,
} from "./apiService";
import {
  accountApiService,
  ChannelContextError,
  publicApiService,
  rejectChannelBootstrap,
  resolveChannelBootstrap,
  isScopedBackOfficeRequest,
  startChannelBootstrap,
} from "./apiTransport";

afterEach(() => {
  vi.unstubAllGlobals();
  setSelectedChannelId(null);
});

describe("channel bootstrap readiness", () => {
  it("waits for readiness and injects the selected channel in both transports", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();
    const adminRequest = adminApiService.get("/api/sponsors");
    const genericRequest = get("/api/products");

    resolveChannelBootstrap(bootstrap, "channel-one");
    await Promise.all([adminRequest, genericRequest]);

    expect(fetchMock).toHaveBeenCalledTimes(2);
    for (const [, options] of fetchMock.mock.calls) {
      expect(new Headers(options.headers).get("X-Channel-Id")).toBe(
        "channel-one",
      );
    }
  });

  it("injects the selected channel for scoped gallery requests", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();
    const request = adminApiService.get("/api/galleries");

    resolveChannelBootstrap(bootstrap, "gallery-channel");
    await request;

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(
      new Headers(fetchMock.mock.calls[0][1].headers).get("X-Channel-Id"),
    ).toBe("gallery-channel");
  });

  it.each([
    "/api/partners",
    "/api/partners/partner-1",
    "/api/campaigns?status=active",
    "/api/campaigns/campaign-1/timeline",
    "/api/script-studio",
    "/api/script-studio/documents/doc-1",
    "/api/script-studio/generate",
    "/api/script-studio/prompts/global",
    "/api/v1/dashboard/quick-actions",
  ])("waits for readiness and scopes %s", async url => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();
    const request = adminApiService.get(url);

    expect(fetchMock).not.toHaveBeenCalled();
    resolveChannelBootstrap(bootstrap, "workspace-channel");
    await request;

    expect(new Headers(fetchMock.mock.calls[0][1].headers).get("X-Channel-Id"))
      .toBe("workspace-channel");
  });

  it.each([
    "/api/script-studio/quota",
    "/api/script-studio/quota/user-1",
    "/api/script-studio/quota/user-1/history",
  ])("keeps quota administration unscoped for %s", async url => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();

    await adminApiService.get(url);

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(new Headers(fetchMock.mock.calls[0][1].headers).get("X-Channel-Id"))
      .toBeNull();
    resolveChannelBootstrap(bootstrap, "unused-channel");
  });

  it("uses segment boundaries around the scoped families", () => {
    expect(isScopedBackOfficeRequest("/api/partners-other")).toBe(false);
    expect(isScopedBackOfficeRequest("/api/campaigns-archive")).toBe(false);
    expect(isScopedBackOfficeRequest("/api/script-studio/quota-other")).toBe(
      true,
    );
    expect(isScopedBackOfficeRequest("/api/script-studio/quotaish")).toBe(
      true,
    );
  });

  it("keeps public and account transports isolated from channel context", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    setSelectedChannelId("admin-channel");

    await publicApiService.get("/api/partners");
    await accountApiService.get("/api/campaigns");

    expect(fetchMock).toHaveBeenCalledTimes(2);
    for (const [, options] of fetchMock.mock.calls) {
      const headers = new Headers(options.headers);
      expect(headers.get("X-Channel-Id")).toBeNull();
      expect(headers.get("Authorization")).toBeNull();
    }
  });

  it("scopes interaction management but leaves the global public account directory unscoped", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    expect(
      isScopedBackOfficeRequest("/api/blog-interactions/accounts/account-1"),
    ).toBe(true);
    expect(isScopedBackOfficeRequest("/api/publicaccounts")).toBe(false);

    await adminApiService.get("/api/publicaccounts");
    const bootstrap = startChannelBootstrap();
    const request = adminApiService.get(
      "/api/blog-interactions/accounts/account-1",
    );
    resolveChannelBootstrap(bootstrap, "interaction-channel");
    await request;

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(
      new Headers(fetchMock.mock.calls[0][1].headers).get("X-Channel-Id"),
    ).toBeNull();
    expect(
      new Headers(fetchMock.mock.calls[1][1].headers).get("X-Channel-Id"),
    ).toBe("interaction-channel");
  });

  it("fails scoped requests locally when no channel is assigned", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();
    resolveChannelBootstrap(bootstrap, null);

    await expect(adminApiService.get("/api/sponsors")).rejects.toMatchObject({
      code: "channel_unavailable",
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("fails scoped requests locally when discovery fails", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    const bootstrap = startChannelBootstrap();
    rejectChannelBootstrap(bootstrap, new Error("discovery failed"));

    await expect(get("/api/products")).rejects.toBeInstanceOf(
      ChannelContextError,
    );
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("does not allow a stale generation to settle the current generation", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    const stale = startChannelBootstrap();
    const current = startChannelBootstrap();
    resolveChannelBootstrap(stale, "stale-channel");
    const request = adminApiService.get("/api/sponsors");
    resolveChannelBootstrap(current, "current-channel");

    await request;
    expect(
      new Headers(fetchMock.mock.calls[0][1].headers).get("X-Channel-Id"),
    ).toBe("current-channel");
  });

  it("coalesces concurrent accessible-channel requests for one generation", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    startChannelBootstrap();

    await Promise.all([
      get("/api/channels/accessible"),
      get("/api/channels/accessible"),
      get("/api/features"),
    ]);

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(
      fetchMock.mock.calls.filter(([url]) =>
        String(url).endsWith("/api/channels/accessible"),
      ),
    ).toHaveLength(1);
  });
});
