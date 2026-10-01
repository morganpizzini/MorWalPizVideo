import { afterEach, describe, expect, it, vi } from "vitest";
import {
  ApiResponseError,
  createApiClient,
  legacyApiService,
  requireSuccessfulResponse,
  getAskCampaign,
  submitAsk,
  reactToAskSubmission,
  getActiveCustomForms,
  getCustomFormByUrl,
  submitCustomFormResponse,
  getEligibleSurveys,
  getSurveyByUrl,
  getPublicFaq,
  getPublicFaqCategories,
  voteFaqAnswer,
  getPublicNavigation,
  adminApiService,
  publicApiService,
  subscribeNewsletter,
  confirmNewsletter,
  unsubscribeNewsletter,
  setSelectedChannelId,
} from "./apiService";
import { fetchSponsors, createSponsorWithImage } from "./adminService";
import endpoints from "./endpoints";
import { getPublicBlog, getPublicBlogPost } from "./blogService";
import {
  getPublicChannelNews,
  getPublicChannelNewsByIdOrSlug,
} from "./channelNewsService";
import { loadChannelMap } from "./videoChannelMap";
import { fetchShopProducts, shopLogin } from "./shopService";

const {
  call,
  get,
  post,
  resetCsrfToken,
  setAuthTokenProvider,
  setRequestCredentialsMode,
} = legacyApiService;

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  resetCsrfToken();
  setAuthTokenProvider(() => null);
  setRequestCredentialsMode("include");
  setSelectedChannelId(null);
});

describe("transport response contracts", () => {
  it("sends the selected channel for admin sponsor reads and mutations", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation((url: string) =>
        Promise.resolve(
          url.endsWith("/csrf")
            ? Response.json({ token: "csrf-token" })
            : Response.json([]),
        ),
      );
    vi.stubGlobal("fetch", fetchMock);
    adminApiService.setSelectedChannelId("selected-channel");

    await fetchSponsors();
    await createSponsorWithImage(new FormData());

    const sponsorRequests = fetchMock.mock.calls.filter(([url]) =>
      url.endsWith("/api/sponsors"),
    );
    expect(sponsorRequests).toHaveLength(2);
    for (const [, options] of sponsorRequests) {
      expect(new Headers(options.headers).get("X-Channel-Id")).toBe(
        "selected-channel",
      );
    }
  });

  it("keeps public sponsor requests anonymous and free of channel headers", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ data: [] }));
    vi.stubGlobal("fetch", fetchMock);
    publicApiService.setSelectedChannelId("injected-channel");

    await publicApiService.get(endpoints.SPONSORS);

    const request = fetchMock.mock.calls[0][1];
    expect(request.credentials).toBe("omit");
    expect(new Headers(request.headers).has("X-Channel-Id")).toBe(false);
    expect(new Headers(request.headers).has("Authorization")).toBe(false);
    expect(new Headers(request.headers).has("X-CSRF-TOKEN")).toBe(false);
  });

  it("routes every active public helper through omit without bearer, CSRF or admin channel", async () => {
    setSelectedChannelId("private-channel");
    setAuthTokenProvider(() => "legacy-token");
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json([])));
    vi.stubGlobal("fetch", fetchMock);
    await Promise.all([
      getAskCampaign("channel", "campaign"),
      submitAsk("channel", "campaign", "question", "captcha"),
      reactToAskSubmission("channel", "campaign", "submission"),
      getActiveCustomForms(),
      getCustomFormByUrl("form"),
      submitCustomFormResponse("form", []),
      getEligibleSurveys(),
      getSurveyByUrl("survey"),
      getPublicFaq(),
      getPublicFaqCategories(),
      voteFaqAnswer("faq", "channel", 1),
      getPublicNavigation(),
      subscribeNewsletter({
        channelId: "channel",
        email: "example@example.test",
        language: "en",
        recaptchaToken: "captcha",
      }),
      confirmNewsletter({ channelId: "channel", token: "confirmation" }),
      unsubscribeNewsletter({ channelId: "channel", token: "unsubscribe" }),
      getPublicBlog(),
      getPublicBlogPost("article"),
      getPublicChannelNews(),
      getPublicChannelNewsByIdOrSlug("news"),
      loadChannelMap(),
    ]);
    expect(fetchMock).toHaveBeenCalledTimes(20);
    for (const [, options] of fetchMock.mock.calls) {
      expect(options.credentials).toBe("omit");
      for (const header of ["Authorization", "X-CSRF-TOKEN", "X-Channel-Id"]) {
        expect(new Headers(options.headers).has(header)).toBe(false);
      }
    }
  });

  it("preserves frozen shop legacy bearer and credentials configuration independently", async () => {
    setAuthTokenProvider(() => "legacy-shop-token");
    setRequestCredentialsMode("same-origin");
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({ data: [] })));
    vi.stubGlobal("fetch", fetchMock);
    await fetchShopProducts();
    await shopLogin({
      email: "example@example.test",
      termsAccepted: true,
      recaptchaToken: "captcha",
    });
    for (const [, options] of fetchMock.mock.calls) {
      expect(options.credentials).toBe("same-origin");
      expect(new Headers(options.headers).get("Authorization")).toBe(
        "Bearer legacy-shop-token",
      );
    }
  });
  it("isolates interleaved public/admin requests and rejects injected auth context", async () => {
    const admin = createApiClient({
      mode: "admin",
      baseUrl: "https://admin.test/",
    });
    const publicClient = createApiClient({
      mode: "public",
      baseUrl: "https://public.test",
    });
    const unauthorized = vi.fn();
    admin.setUnauthorizedHandler(unauthorized);
    admin.setSelectedChannelId("admin-channel");
    admin.setAuthTokenProvider(() => "admin-token");
    publicClient.setAuthTokenProvider(() => "public-token");
    publicClient.setSelectedChannelId("injected-channel");
    publicClient.setRequestCredentialsMode("include");
    publicClient.setUnauthorizedHandler(unauthorized);
    const fetchMock = vi
      .fn()
      .mockImplementation((url: string) =>
        Promise.resolve(
          Response.json(
            url.endsWith("/csrf") ? { token: "admin-csrf" } : { data: [] },
          ),
        ),
      );
    vi.stubGlobal("fetch", fetchMock);
    await Promise.all([
      admin.post("/api/videos", { title: "admin" }, "", {
        Authorization: "Bearer injected",
      }),
      publicClient.post("/api/videos", { title: "public" }, "", {
        authorization: "Bearer injected",
        Cookie: "auth_token=injected",
        "X-Channel-Id": "injected",
        "X-CSRF-TOKEN": "injected",
      }),
    ]);
    const publicRequest = fetchMock.mock.calls.find(([url]) =>
      url.startsWith("https://public"),
    )![1];
    expect(publicRequest.credentials).toBe("omit");
    for (const header of [
      "Authorization",
      "Cookie",
      "X-Channel-Id",
      "X-CSRF-TOKEN",
    ]) {
      expect(new Headers(publicRequest.headers).has(header)).toBe(false);
    }
    const adminRequest = fetchMock.mock.calls.find(
      ([url]) => url === "https://admin.test/api/videos",
    )![1];
    expect(adminRequest.credentials).toBe("include");
    expect(new Headers(adminRequest.headers).has("Authorization")).toBe(false);
    expect(new Headers(adminRequest.headers).get("X-Channel-Id")).toBe(
      "admin-channel",
    );
    expect(new Headers(adminRequest.headers).get("X-CSRF-TOKEN")).toBe(
      "admin-csrf",
    );
    fetchMock.mockImplementation(() =>
      Promise.resolve(Response.json({ errors: ["expired"] }, { status: 401 })),
    );
    await publicClient.get("/api/videos");
    expect(unauthorized).not.toHaveBeenCalled();
    await admin.get("/api/videos");
    expect(unauthorized).toHaveBeenCalledOnce();
  });

  it("owns SSR request base URLs, CSRF, channel and unauthorized callbacks independently", async () => {
    const first = createApiClient({
      mode: "admin",
      baseUrl: "https://first.test",
    });
    const second = createApiClient({
      mode: "admin",
      baseUrl: "https://second.test",
    });
    first.setSelectedChannelId("first");
    second.setSelectedChannelId("second");
    const fetchMock = vi
      .fn()
      .mockImplementation((url: string) =>
        Promise.resolve(
          Response.json(
            url.endsWith("/csrf") ? { token: url } : { saved: true },
          ),
        ),
      );
    vi.stubGlobal("fetch", fetchMock);
    await Promise.all([
      first.post("/api/videos", {}),
      second.post("/api/videos", {}),
    ]);
    for (const name of ["first", "second"]) {
      const request = fetchMock.mock.calls.find(
        ([url]) => url === `https://${name}.test/api/videos`,
      )![1];
      expect(new Headers(request.headers).get("X-Channel-Id")).toBe(name);
      expect(new Headers(request.headers).get("X-CSRF-TOKEN")).toBe(
        `https://${name}.test/api/auth/csrf`,
      );
    }
    expect(
      createApiClient({ mode: "admin" }).getSelectedChannelId(),
    ).toBeNull();
    const firstUnauthorized = vi.fn();
    const secondUnauthorized = vi.fn();
    first.setUnauthorizedHandler(firstUnauthorized);
    second.setUnauthorizedHandler(secondUnauthorized);
    fetchMock.mockImplementation(() =>
      Promise.resolve(Response.json({}, { status: 401 })),
    );
    await first.get("/api/videos");
    expect(firstUnauthorized).toHaveBeenCalledOnce();
    expect(secondUnauthorized).not.toHaveBeenCalled();
    await second.get("/api/videos");
    expect(secondUnauthorized).toHaveBeenCalledOnce();
  });

  it("preserves runtime/build URL priority and explicit request-local URL overrides", async () => {
    vi.stubEnv("VITE_API_BASE_URL", "https://build.test");
    vi.stubGlobal("window", {
      ENV: {
        VITE_API_BASE_URL: "https://runtime.test",
        API_BASE_URL: "https://fallback.test",
      },
    });
    const client = createApiClient({ mode: "public" });
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(Response.json({})));
    vi.stubGlobal("fetch", fetchMock);
    await client.get("api/videos", { page: 2 });
    expect(fetchMock.mock.calls[0][0]).toBe(
      "https://runtime.test/api/videos?page=2",
    );
    vi.stubGlobal("window", { ENV: { API_BASE_URL: "https://fallback.test" } });
    await client.get("api/videos");
    expect(fetchMock.mock.calls[1][0]).toBe("https://fallback.test/api/videos");
    vi.stubGlobal("window", undefined);
    await client.get("api/videos");
    expect(fetchMock.mock.calls[2][0]).toBe("https://build.test/api/videos");
    await createApiClient({
      mode: "public",
      baseUrl: "https://request.test/",
    }).get("api/videos");
    expect(fetchMock.mock.calls[3][0]).toBe("https://request.test/api/videos");
    vi.stubEnv("VITE_API_BASE_URL", "");
    await client.get("api/videos");
    expect(fetchMock.mock.calls[4][0]).toBe("/api/videos");
  });

  it("retries failed CSRF acquisition and never runs unauthorized recovery for login", async () => {
    const client = createApiClient({ mode: "admin", baseUrl: "" });
    const unauthorized = vi.fn();
    client.setUnauthorizedHandler(unauthorized);
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 503 }))
      .mockResolvedValueOnce(Response.json({ token: "retry-token" }))
      .mockResolvedValueOnce(Response.json({ saved: true }))
      .mockResolvedValueOnce(
        Response.json({ message: "invalid" }, { status: 401 }),
      )
      .mockResolvedValueOnce(new Response("expired", { status: 401 }));
    vi.stubGlobal("fetch", fetchMock);
    await expect(client.post("/api/videos", {})).rejects.toThrow(
      "Unable to acquire CSRF token (503)",
    );
    await client.post("/api/videos", {});
    await client.post("/api/auth/login", { username: "admin" });
    expect(unauthorized).not.toHaveBeenCalled();
    expect(await client.get("/api/videos")).toEqual({
      errors: ["Authentication failed"],
      status: 401,
    });
    expect(unauthorized).toHaveBeenCalledOnce();
  });
  it("unwraps data and preserves full-response and empty responses", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(Response.json({ data: [1], total: 1 }))
        .mockResolvedValueOnce(Response.json({ data: [1], total: 1 }))
        .mockResolvedValueOnce(new Response(null, { status: 204 })),
    );
    expect(await get("/api/items")).toEqual([1]);
    expect(await get("/api/items", undefined, "", true)).toEqual({
      data: [1],
      total: 1,
    });
    expect(await get("/api/items")).toEqual({});
  });

  it.each([400, 404, 409, 429, 503])(
    "preserves error-return contracts for %s",
    async (status) => {
      vi.stubGlobal(
        "fetch",
        vi
          .fn()
          .mockResolvedValue(Response.json({ errors: ["failed"] }, { status })),
      );
      const response = await get("/api/items");
      if (status === 429) expect(response).toEqual({ errors: ["failed"] });
      else
        expect(response).toMatchObject({ status, errors: expect.any(Array) });
    },
  );

  it("preserves blobs and network rejection", async () => {
    const failure = new TypeError("offline");
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(new Response("download"))
        .mockRejectedValueOnce(failure),
    );
    expect(
      await (await call("/api/file", "GET", {}, "", undefined, true)).text(),
    ).toBe("download");
    await expect(get("/api/items")).rejects.toBe(failure);
  });

  it("shares CSRF within a session and reacquires after reset", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation((url: string) =>
        Promise.resolve(
          Response.json(
            url.endsWith("/csrf") ? { token: "csrf" } : { saved: true },
          ),
        ),
      );
    vi.stubGlobal("fetch", fetchMock);
    await post("/api/items", { title: "one" });
    await post("/api/items", { title: "two" });
    resetCsrfToken();
    await post("/api/items", { title: "three" });
    expect(
      fetchMock.mock.calls.filter(([url]) => url.endsWith("/csrf")),
    ).toHaveLength(2);
    expect(
      new Headers(fetchMock.mock.calls[1][1].headers).get("X-CSRF-TOKEN"),
    ).toBe("csrf");
  });
});

describe("requireSuccessfulResponse", () => {
  it("accepts a successful response", () => {
    const response = { status: 204 };

    expect(requireSuccessfulResponse(response)).toBe(response);
  });

  it("rejects failed responses and preserves field errors", () => {
    expect(() =>
      requireSuccessfulResponse({
        status: 400,
        errors: ["The Email field is not a valid e-mail address."],
        fieldErrors: {
          Email: ["The Email field is not a valid e-mail address."],
          Token: ["The Token field is required."],
        },
      }),
    ).toThrow(
      "Email: The Email field is not a valid e-mail address.\nToken: The Token field is required.",
    );

    try {
      requireSuccessfulResponse({
        status: 400,
        errors: ["validation failed"],
        fieldErrors: { Email: ["invalid"] },
      });
    } catch (error) {
      expect(error).toBeInstanceOf(ApiResponseError);
      expect((error as ApiResponseError).status).toBe(400);
      expect((error as ApiResponseError).fieldErrors).toEqual({
        Email: ["invalid"],
      });
    }
  });
});
