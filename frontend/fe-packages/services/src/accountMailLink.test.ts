import { describe, expect, it, vi } from "vitest";
import { captureAccountMailLink } from "./accountMailLink";

describe("account email link capture", () => {
  it("keeps the token only in memory and removes the fragment before route creation", () => {
    const replaceState = vi.fn();
    const token = "A".repeat(64);
    expect(captureAccountMailLink({
      pathname: "/account/reset-password", search: "?source=email",
      hash: `#accountId=${"b".repeat(24)}&token=${token}`,
    }, { replaceState })).toEqual({ accountId: "b".repeat(24), token });
    expect(replaceState).toHaveBeenCalledWith(null, "", "/account/reset-password?source=email");
    expect(JSON.stringify(replaceState.mock.calls)).not.toContain(token);
  });

  it("scrubs invalid fragments but does not touch unrelated application hashes", () => {
    const replaceState = vi.fn();
    expect(captureAccountMailLink({ pathname: "/account/confirm-email", search: "", hash: "#token=invalid" }, { replaceState })).toBeNull();
    expect(replaceState).toHaveBeenCalledOnce();
    expect(captureAccountMailLink({ pathname: "/blog", search: "", hash: "#comments" }, { replaceState })).toBeNull();
    expect(replaceState).toHaveBeenCalledOnce();
  });
});
