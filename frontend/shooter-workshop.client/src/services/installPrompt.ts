import { useSyncExternalStore } from "react";

export type InstallPromptOutcome = "accepted" | "dismissed" | "failed";
type BrowserInstallPromptOutcome = Exclude<InstallPromptOutcome, "failed">;

type BeforeInstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: BrowserInstallPromptOutcome }>;
};

export interface InstallPromptState {
  canInstall: boolean;
  dismissed: boolean;
  installed: boolean;
  installError: boolean;
}

const DISMISSED_STORAGE_KEY = "shooter-workshop.install-prompt-dismissed.v1";
const listeners = new Set<() => void>();
let deferredPrompt: BeforeInstallPromptEvent | undefined;
let state: InstallPromptState = {
  canInstall: false,
  dismissed: readDismissedState(),
  installed: isStandaloneDisplayMode(),
  installError: false,
};

function readDismissedState(): boolean {
  if (typeof window === "undefined") return false;

  try {
    return window.localStorage.getItem(DISMISSED_STORAGE_KEY) === "true";
  } catch (error) {
    console.warn("Unable to read Workshop install prompt state", error);
    return false;
  }
}

function notify(): void {
  listeners.forEach(listener => listener());
}

function updateState(nextState: Partial<InstallPromptState>): void {
  state = { ...state, ...nextState };
  notify();
}

function isStandaloneDisplayMode(): boolean {
  if (typeof window === "undefined") return false;

  return (
    window.matchMedia?.("(display-mode: standalone)").matches === true ||
    (typeof navigator !== "undefined" &&
      (navigator as Navigator & { standalone?: boolean }).standalone === true)
  );
}

function handleBeforeInstallPrompt(event: Event): void {
  event.preventDefault();
  deferredPrompt = event as BeforeInstallPromptEvent;
  updateState({ canInstall: true, installError: false });
}

function handleAppInstalled(): void {
  deferredPrompt = undefined;
  updateState({ canInstall: false, installed: true, installError: false });
}

if (typeof window !== "undefined") {
  window.addEventListener("beforeinstallprompt", handleBeforeInstallPrompt);
  window.addEventListener("appinstalled", handleAppInstalled);
}

export function subscribeToInstallPrompt(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getInstallPromptSnapshot(): InstallPromptState {
  return state;
}

export function useInstallPrompt(): InstallPromptState & {
  dismiss: () => void;
  install: () => Promise<InstallPromptOutcome | undefined>;
  reopen: () => void;
} {
  const currentState = useSyncExternalStore(
    subscribeToInstallPrompt,
    getInstallPromptSnapshot,
    getInstallPromptSnapshot,
  );

  return {
    ...currentState,
    dismiss: () => {
      try {
        window.localStorage.setItem(DISMISSED_STORAGE_KEY, "true");
      } catch (error) {
        console.warn("Unable to persist Workshop install prompt dismissal", error);
      }
      updateState({ dismissed: true, installError: false });
    },
    install: async () => {
      if (!deferredPrompt) return undefined;

      const promptEvent = deferredPrompt;
      deferredPrompt = undefined;
      updateState({ canInstall: false });
      try {
        await promptEvent.prompt();
        const choice = await promptEvent.userChoice;
        if (choice.outcome === "accepted") updateState({ dismissed: false });
        return choice.outcome;
      } catch (error) {
        console.error("Unable to start the Workshop installation prompt", error);
        updateState({ installError: true });
        return "failed";
      }
    },
    reopen: () => updateState({ dismissed: false }),
  };
}

export function resetInstallPromptForTests(): void {
  deferredPrompt = undefined;
  state = { canInstall: false, dismissed: false, installed: false, installError: false };
  notify();
}
