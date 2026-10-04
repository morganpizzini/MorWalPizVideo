import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const savePushSubscription = vi.fn();
const getPushPublicKey = vi.fn();
const getPushSubscriptionSettings = vi.fn();
const revokePushSubscription = vi.fn();

vi.mock("./pushService", () => ({
  getPushPublicKey: (...args: unknown[]) => getPushPublicKey(...args),
  savePushSubscription: (...args: unknown[]) => savePushSubscription(...args),
  getPushSubscriptionSettings: (...args: unknown[]) =>
    getPushSubscriptionSettings(...args),
  revokePushSubscription: (...args: unknown[]) =>
    revokePushSubscription(...args),
}));

import {
  clearStoredPushCredential,
  dismissPushPrompt,
  getPushSupport,
  readStoredPushCredential,
  readPushPromptRecord,
  recordPushPromptDecision,
  requestPushOptIn,
  revokePushOptIn,
  shouldShowPushPrompt,
  urlBase64ToUint8Array,
} from "./pushNotifications";

class MemoryStorage {
  private readonly values = new Map<string, string>();
  getItem(key: string) {
    return this.values.get(key) ?? null;
  }
  setItem(key: string, value: string) {
    this.values.set(key, value);
  }
  removeItem(key: string) {
    this.values.delete(key);
  }
  clear() {
    this.values.clear();
  }
  key(index: number) {
    return [...this.values.keys()][index] ?? null;
  }
  get length() {
    return this.values.size;
  }
}

const requestPermission = vi.fn();
const subscribe = vi.fn();
const getSubscription = vi.fn();
const unsubscribe = vi.fn();

function installBrowser(permission: NotificationPermission = "default") {
  const registration = {
    pushManager: { subscribe, getSubscription },
  };
  vi.stubGlobal("window", {
    localStorage: new MemoryStorage(),
    PushManager: class {},
    Notification: { permission, requestPermission, maxActions: 2 },
    atob: (value: string) => Buffer.from(value, "base64").toString("binary"),
  });
  vi.stubGlobal("navigator", {
    serviceWorker: { ready: Promise.resolve(registration) },
  });
}

beforeEach(() => {
  vi.clearAllMocks();
  installBrowser();
  getPushPublicKey.mockResolvedValue({ publicKey: "BPublicKey" });
  requestPermission.mockResolvedValue("granted");
  getSubscription.mockResolvedValue(null);
  subscribe.mockResolvedValue({
    endpoint: "https://push.example.com/send/abc",
    toJSON: () => ({
      endpoint: "https://push.example.com/send/abc",
      keys: { p256dh: "key", auth: "auth" },
    }),
    unsubscribe,
  });
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("getPushSupport", () => {
  it("reports unsupported when the browser APIs are missing", () => {
    vi.unstubAllGlobals();
    expect(getPushSupport()).toEqual({
      supported: false,
      permission: "unsupported",
    });
  });

  it("reports the current browser permission when supported", () => {
    expect(getPushSupport()).toEqual({
      supported: true,
      permission: "default",
    });
  });
});

describe("shouldShowPushPrompt", () => {
  it("shows the prompt once and never again after a dismissal", () => {
    expect(shouldShowPushPrompt()).toBe(true);
    dismissPushPrompt();
    expect(shouldShowPushPrompt()).toBe(false);
  });

  it("reflects an application decision in the default browser scope", () => {
    recordPushPromptDecision("dismissed", "morwalpizvideo");

    expect(
      window.localStorage.getItem("mwp.push.prompt.morwalpizvideo"),
    ).not.toBeNull();
    expect(
      window.localStorage.getItem("mwp.push.prompt.default"),
    ).not.toBeNull();
    expect(readPushPromptRecord("morwalpizvideo")).toMatchObject({
      decision: "dismissed",
    });
    expect(readPushPromptRecord()).toMatchObject({
      decision: "dismissed",
    });
    expect(shouldShowPushPrompt("morwalpizvideo")).toBe(false);
    expect(shouldShowPushPrompt()).toBe(false);
  });

  it("keeps no-argument decisions in the default scope", () => {
    recordPushPromptDecision("dismissed");

    expect(
      window.localStorage.getItem("mwp.push.prompt.default"),
    ).not.toBeNull();
    expect(
      window.localStorage.getItem("mwp.push.prompt.morwalpizvideo"),
    ).toBeNull();
    expect(readPushPromptRecord()).toMatchObject({ decision: "dismissed" });
    expect(readPushPromptRecord("morwalpizvideo")).toMatchObject({
      decision: "dismissed",
    });
  });

  it("stays hidden when permission was already decided in the browser", () => {
    installBrowser("denied");
    expect(shouldShowPushPrompt()).toBe(false);
  });
});

describe("requestPushOptIn", () => {
  it("subscribes and stores the minted credential for later anonymous management", async () => {
    savePushSubscription.mockResolvedValue({
      channelIds: ["channel-1"],
      credential: "a".repeat(64),
    });

    const result = await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-1"],
    });

    expect(result.status).toBe("subscribed");
    expect(savePushSubscription).toHaveBeenCalledWith(
      expect.objectContaining({
        endpoint: "https://push.example.com/send/abc",
        channelIds: ["channel-1"],
        applicationKey: "morwalpizvideo",
        credential: undefined,
      }),
    );
    expect(readStoredPushCredential()).toEqual({
      endpoint: "https://push.example.com/send/abc",
      credential: "a".repeat(64),
    });
    expect(shouldShowPushPrompt()).toBe(false);
  });

  it("replays the stored credential when the same endpoint is re-subscribed", async () => {
    savePushSubscription.mockResolvedValueOnce({
      channelIds: ["channel-1"],
      credential: "b".repeat(64),
    });
    await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-1"],
    });

    savePushSubscription.mockResolvedValueOnce({ channelIds: ["channel-2"] });
    await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-2"],
    });

    expect(savePushSubscription).toHaveBeenLastCalledWith(
      expect.objectContaining({ credential: "b".repeat(64) }),
    );
  });

  it("records a dismissal when the browser permission is refused", async () => {
    requestPermission.mockResolvedValue("denied");

    const result = await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-1"],
    });

    expect(result.status).toBe("denied");
    expect(savePushSubscription).not.toHaveBeenCalled();
    expect(shouldShowPushPrompt()).toBe(false);
  });

  it("reports unavailable when the server has no VAPID public key", async () => {
    getPushPublicKey.mockRejectedValue(new Error("503"));

    const result = await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-1"],
    });

    expect(result.status).toBe("unavailable");
  });
});

describe("revokePushOptIn", () => {
  it("clears the stored credential and unsubscribes the browser", async () => {
    savePushSubscription.mockResolvedValue({
      channelIds: ["channel-1"],
      credential: "c".repeat(64),
    });
    await requestPushOptIn({
      applicationKey: "morwalpizvideo",
      channelIds: ["channel-1"],
    });
    getSubscription.mockResolvedValue({ unsubscribe });
    revokePushSubscription.mockResolvedValue(undefined);

    await expect(revokePushOptIn()).resolves.toBe(true);
    expect(unsubscribe).toHaveBeenCalled();
    expect(readStoredPushCredential()).toBeNull();
  });

  it("is a no-op without a stored credential", async () => {
    clearStoredPushCredential();
    await expect(revokePushOptIn()).resolves.toBe(false);
    expect(revokePushSubscription).not.toHaveBeenCalled();
  });
});

describe("urlBase64ToUint8Array", () => {
  it("decodes url-safe base64 VAPID keys", () => {
    expect([...urlBase64ToUint8Array("-_8")]).toEqual([251, 255]);
  });
});
