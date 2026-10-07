import type { PushSubscriptionState } from "@morwalpizvideo/models";
import {
  getPushPublicKey,
  getPushSubscriptionSettings,
  revokePushSubscription,
  savePushSubscription,
} from "./pushService";

/**
 * Framework-agnostic Web Push opt-in helpers shared by the public applications.
 *
 * Subscriptions are anonymous: the browser endpoint is the identity and a high-entropy credential minted by the
 * server on first subscribe is kept in local storage so the visitor can manage or revoke the subscription later
 * without an account. Nothing here touches the DOM, so it stays safe to import from SSR bundles as long as the
 * functions are only called from effects or event handlers.
 */

const CREDENTIAL_STORAGE_KEY = "mwp.push.credential";
const PROMPT_STORAGE_KEY = "mwp.push.prompt";
function promptStorageKey(applicationKey = "default"): string {
  return `${PROMPT_STORAGE_KEY}.${applicationKey.trim().toLowerCase() || "default"}`;
}

export type PushPromptDecision = "accepted" | "dismissed";

export type PushPromptRecord = Readonly<{
  decision: PushPromptDecision;
  decidedAt: string;
}>;

export type StoredPushCredential = Readonly<{
  endpoint: string;
  credential: string;
}>;

export type PushSupport = Readonly<{
  supported: boolean;
  permission: NotificationPermission | "unsupported";
}>;

function storage(): Storage | null {
  try {
    if (typeof window === "undefined" || !window.localStorage) return null;
    // SSR shims expose a no-op localStorage; a round-trip probe keeps us honest.
    return window.localStorage;
  } catch {
    return null;
  }
}

function readJson<T>(key: string): T | null {
  const store = storage();
  if (!store) return null;
  try {
    const raw = store.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

function writeJson(key: string, value: unknown): void {
  const store = storage();
  if (!store) return;
  try {
    store.setItem(key, JSON.stringify(value));
  } catch {
    /* Private browsing or a quota failure must never break the page. */
  }
}

/** True only when the browser exposes every API the opt-in flow needs. */
export function getPushSupport(): PushSupport {
  if (
    typeof window === "undefined" ||
    typeof navigator === "undefined" ||
    !("serviceWorker" in navigator) ||
    typeof window.PushManager === "undefined" ||
    typeof window.Notification === "undefined"
  ) {
    return { supported: false, permission: "unsupported" };
  }
  return { supported: true, permission: window.Notification.permission };
}

export function readPushPromptRecord(
  applicationKey?: string,
): PushPromptRecord | null {
  const applicationPromptRecord = readJson<PushPromptRecord>(
    promptStorageKey(applicationKey),
  );
  if (applicationPromptRecord !== null || applicationKey === undefined) {
    return applicationPromptRecord;
  }
  return readJson<PushPromptRecord>(promptStorageKey());
}

export function recordPushPromptDecision(
  decision: PushPromptDecision,
  applicationKey?: string,
): void {
  const record = {
    decision,
    decidedAt: new Date().toISOString(),
  } satisfies PushPromptRecord;
  const applicationPromptStorageKey = promptStorageKey(applicationKey);

  writeJson(applicationPromptStorageKey, record);
  if (
    applicationKey !== undefined &&
    applicationPromptStorageKey !== promptStorageKey()
  ) {
    writeJson(promptStorageKey(), record);
  }
}

export function readStoredPushCredential(): StoredPushCredential | null {
  const stored = readJson<StoredPushCredential>(CREDENTIAL_STORAGE_KEY);
  return stored?.endpoint && stored.credential ? stored : null;
}

export function clearStoredPushCredential(): void {
  storage()?.removeItem(CREDENTIAL_STORAGE_KEY);
}

/**
 * The prompt is shown once per browser: never when push is unsupported, already decided, or already granted or
 * blocked at the browser level.
 */
export function shouldShowPushPrompt(applicationKey?: string): boolean {
  const support = getPushSupport();
  if (!support.supported || support.permission !== "default") return false;
  return readPushPromptRecord(applicationKey) === null;
}

export function dismissPushPrompt(applicationKey?: string): void {
  recordPushPromptDecision("dismissed", applicationKey);
}

export function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const rawData = window.atob(base64);
  const output = new Uint8Array(rawData.length);
  for (let index = 0; index < rawData.length; index += 1) {
    output[index] = rawData.charCodeAt(index);
  }
  return output;
}

export type PushOptInOptions = Readonly<{
  applicationKey: string;
  channelIds: readonly string[];
  language?: string;
}>;

export type PushOptInResult = Readonly<{
  status: "subscribed" | "denied" | "unsupported" | "unavailable";
  reason?:
    | "configuration"
    | "permission"
    | "service-worker"
    | "subscription"
    | "persistence";
  state?: PushSubscriptionState;
}>;

function logPushFailure(stage: PushOptInResult["reason"]): void {
  // Keep diagnostics useful without logging the VAPID key, endpoint, or subscription keys.
  console.error("[push] activation failed", { stage });
}

async function resolveRegistration(): Promise<ServiceWorkerRegistration | null> {
  try {
    return (await navigator.serviceWorker.ready) ?? null;
  } catch {
    return null;
  }
}

/**
 * Requests permission, subscribes through the active service worker and persists the subscription server-side.
 * Re-subscribing an endpoint replays the stored credential so the server can authorise the change.
 */
export async function requestPushOptIn(
  options: PushOptInOptions,
): Promise<PushOptInResult> {
  const support = getPushSupport();
  if (!support.supported) return { status: "unsupported" };

  let permission: NotificationPermission;
  try {
    // Keep this as the first awaited operation: browsers can reject permission
    // requests after the click's transient user activation has been consumed by
    // an earlier network await.
    permission = await window.Notification.requestPermission();
  } catch {
    logPushFailure("permission");
    return { status: "unavailable", reason: "permission" };
  }
  if (permission !== "granted") {
    recordPushPromptDecision("dismissed", options.applicationKey);
    return { status: "denied" };
  }

  let publicKey: string;
  try {
    publicKey = (await getPushPublicKey()).publicKey;
  } catch {
    logPushFailure("configuration");
    return { status: "unavailable", reason: "configuration" };
  }
  if (!publicKey) {
    logPushFailure("configuration");
    return { status: "unavailable", reason: "configuration" };
  }

  const registration = await resolveRegistration();
  if (!registration) {
    logPushFailure("service-worker");
    return { status: "unavailable", reason: "service-worker" };
  }

  let subscription: PushSubscription;
  try {
    const existing = await registration.pushManager.getSubscription();
    subscription =
      existing ??
      (await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(
          publicKey,
        ) as unknown as BufferSource,
      }));
  } catch {
    logPushFailure("subscription");
    return { status: "unavailable", reason: "subscription" };
  }

  const payload = subscription.toJSON();
  const endpoint = payload.endpoint ?? subscription.endpoint;
  const stored = readStoredPushCredential();

  let state: PushSubscriptionState;
  try {
    state = await savePushSubscription({
      endpoint,
      keys: {
        p256dh: payload.keys?.p256dh ?? "",
        auth: payload.keys?.auth ?? "",
      },
      channelIds: options.channelIds,
      applicationKey: options.applicationKey,
      language: options.language,
      credential: stored?.endpoint === endpoint ? stored.credential : undefined,
    });
  } catch {
    logPushFailure("persistence");
    return { status: "unavailable", reason: "persistence" };
  }

  if (state.credential) {
    writeJson(CREDENTIAL_STORAGE_KEY, {
      endpoint,
      credential: state.credential,
    } satisfies StoredPushCredential);
  }
  recordPushPromptDecision("accepted", options.applicationKey);
  return { status: "subscribed", state };
}

/** Reads the current anonymous subscription using the locally stored credential. */
export async function loadPushSettings(): Promise<PushSubscriptionState | null> {
  const stored = readStoredPushCredential();
  if (!stored) return null;
  try {
    return await getPushSubscriptionSettings(
      stored.endpoint,
      stored.credential,
    );
  } catch {
    return null;
  }
}

/** Updates the subscribed channels for the current browser. */
export async function updatePushChannels(
  options: PushOptInOptions,
): Promise<PushSubscriptionState | null> {
  const stored = readStoredPushCredential();
  if (!stored) return null;
  const registration = await resolveRegistration();
  const subscription = await registration?.pushManager.getSubscription();
  const payload = subscription?.toJSON();
  if (!payload?.endpoint) return null;

  return savePushSubscription({
    endpoint: payload.endpoint,
    keys: {
      p256dh: payload.keys?.p256dh ?? "",
      auth: payload.keys?.auth ?? "",
    },
    channelIds: options.channelIds,
    applicationKey: options.applicationKey,
    language: options.language,
    credential: stored.credential,
  });
}

/** Revokes server-side consent and unsubscribes the browser. */
export async function revokePushOptIn(): Promise<boolean> {
  const stored = readStoredPushCredential();
  if (!stored) return false;
  try {
    await revokePushSubscription(stored.endpoint, stored.credential);
  } catch {
    return false;
  }
  const registration = await resolveRegistration();
  const subscription = await registration?.pushManager.getSubscription();
  await subscription?.unsubscribe();
  clearStoredPushCredential();
  return true;
}
