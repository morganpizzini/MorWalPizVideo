import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getPublicAccountErrorMessage,
  getPublicAccountSession,
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
});
