import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@testing-library/react";
import {
  getInstallPromptSnapshot,
  resetInstallPromptForTests,
  subscribeToInstallPrompt,
  useInstallPrompt,
} from "./installPrompt";

function dispatchInstallEvent(outcome: "accepted" | "dismissed" = "accepted") {
  const prompt = vi.fn().mockResolvedValue(undefined);
  const event = Object.assign(new Event("beforeinstallprompt"), {
    prompt,
    userChoice: Promise.resolve({ outcome }),
  });
  window.dispatchEvent(event);
  return prompt;
}

describe("installPrompt", () => {
  beforeEach(() => {
    localStorage.clear();
    resetInstallPromptForTests();
  });

  afterEach(() => {
    window.dispatchEvent(new Event("appinstalled"));
  });

  it("captures the browser event and notifies subscribers", () => {
    const listener = vi.fn();
    const unsubscribe = subscribeToInstallPrompt(listener);

    dispatchInstallEvent();

    expect(getInstallPromptSnapshot()).toMatchObject({ canInstall: true, dismissed: false });
    expect(listener).toHaveBeenCalled();
    unsubscribe();
  });

  it("prompts only through an explicit install action", async () => {
    const prompt = dispatchInstallEvent();
    const { result } = renderHook(() => useInstallPrompt());

    await act(async () => {
      await expect(result.current.install()).resolves.toBe("accepted");
    });

    expect(prompt).toHaveBeenCalledOnce();
    expect(getInstallPromptSnapshot().canInstall).toBe(false);
  });

  it("reports a rejected browser prompt and consumes the one-shot event", async () => {
    const prompt = vi.fn().mockRejectedValue(new Error("prompt unavailable"));
    const error = vi.spyOn(console, "error").mockImplementation(() => undefined);
    window.dispatchEvent(
      Object.assign(new Event("beforeinstallprompt"), {
        prompt,
        userChoice: Promise.resolve({ outcome: "accepted" as const }),
      }),
    );
    const { result } = renderHook(() => useInstallPrompt());

    await act(async () => {
      await expect(result.current.install()).resolves.toBe("failed");
    });

    expect(prompt).toHaveBeenCalledOnce();
    expect(getInstallPromptSnapshot()).toMatchObject({ canInstall: false, installError: true });
    await expect(result.current.install()).resolves.toBeUndefined();
    error.mockRestore();
  });

  it("reports a failed browser choice and accepts a fresh install event", async () => {
    const error = vi.spyOn(console, "error").mockImplementation(() => undefined);
    window.dispatchEvent(
      Object.assign(new Event("beforeinstallprompt"), {
        prompt: vi.fn().mockResolvedValue(undefined),
        userChoice: Promise.reject(new Error("choice unavailable")),
      }),
    );
    const { result } = renderHook(() => useInstallPrompt());

    await act(async () => {
      await expect(result.current.install()).resolves.toBe("failed");
    });
    expect(getInstallPromptSnapshot().installError).toBe(true);

    dispatchInstallEvent();
    expect(getInstallPromptSnapshot()).toMatchObject({ canInstall: true, installError: false });
    error.mockRestore();
  });

  it("persists dismissal and allows a deliberate reopen", () => {
    dispatchInstallEvent();
    const { result } = renderHook(() => useInstallPrompt());

    act(() => result.current.dismiss());
    expect(getInstallPromptSnapshot().dismissed).toBe(true);
    expect(localStorage.getItem("shooter-workshop.install-prompt-dismissed.v1")).toBe("true");

    act(() => result.current.reopen());
    expect(getInstallPromptSnapshot().dismissed).toBe(false);
  });

  it("hides the invitation after the app is installed", () => {
    dispatchInstallEvent();
    window.dispatchEvent(new Event("appinstalled"));

    expect(getInstallPromptSnapshot()).toMatchObject({
      canInstall: false,
      installed: true,
      installError: false,
    });
  });
});
