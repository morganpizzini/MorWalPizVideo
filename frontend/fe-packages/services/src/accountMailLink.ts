import type { PublicAccountTokenRequest } from "@morwalpizvideo/models";

export function captureAccountMailLink(location: Pick<Location, "pathname" | "search" | "hash">, history: Pick<History, "replaceState">): PublicAccountTokenRequest | null {
  if (!["/account/reset-password", "/account/confirm-email"].includes(location.pathname)) return null;
  const fragment = new URLSearchParams(location.hash.slice(1));
  const accountId = fragment.get("accountId") ?? "";
  const token = fragment.get("token") ?? "";
  history.replaceState(null, "", location.pathname + location.search);
  return /^[a-fA-F0-9]{24}$/.test(accountId) && /^[a-fA-F0-9]{64}$/.test(token)
    ? { accountId, token } : null;
}

// Imported before the router and third-party integrations; never stored on window or in storage.
let captured = typeof window === "undefined" ? null : captureAccountMailLink(window.location, window.history);
if (typeof document !== "undefined" && ["/account/reset-password", "/account/confirm-email"].includes(window.location.pathname)) {
  const meta = document.createElement("meta");
  meta.name = "referrer";
  meta.content = "no-referrer";
  document.head.appendChild(meta);
}
export const getAccountMailLink = (): PublicAccountTokenRequest | null => captured;
export const clearAccountMailLink = (): void => { captured = null; };
