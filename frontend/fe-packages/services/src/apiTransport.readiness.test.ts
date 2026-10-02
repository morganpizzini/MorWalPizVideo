import { afterEach, describe, expect, it, vi } from "vitest";
import { adminApiService, get, setSelectedChannelId } from "./apiService";
import {
  ChannelContextError,
  rejectChannelBootstrap,
  resolveChannelBootstrap,
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
