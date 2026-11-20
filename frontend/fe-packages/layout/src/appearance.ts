import { useSyncExternalStore, type ReactNode } from "react";

export const APPEARANCE_STORAGE_KEY = "morwalpiz.appearance.v1";
const SYSTEM_PREFERENCE_QUERY = "(prefers-color-scheme: light)";

export type AppearancePreference = "system" | "light" | "dark";
export type ResolvedAppearance = Exclude<AppearancePreference, "system">;

export interface AppearanceSnapshot {
  preference: AppearancePreference;
  resolved: ResolvedAppearance;
  persistenceUnavailable: boolean;
}

const serverSnapshot: AppearanceSnapshot = Object.freeze({
  preference: "system",
  resolved: "dark",
  persistenceUnavailable: false,
});

let snapshot = serverSnapshot;
let initialized = false;
let subscribers = new Set<() => void>();
let mediaQuery: MediaQueryList | null = null;
let removeMediaListener: (() => void) | null = null;
let removeStorageListener: (() => void) | null = null;

export function resolveAppearance(
  preference: AppearancePreference,
  systemPrefersLight: boolean | null,
): ResolvedAppearance {
  if (preference === "light" || preference === "dark") return preference;
  return systemPrefersLight === true ? "light" : "dark";
}

function getSystemPreference(): boolean | null {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return null;
  }

  try {
    return window.matchMedia(SYSTEM_PREFERENCE_QUERY).matches;
  } catch {
    return null;
  }
}

function applyDocumentTheme(resolved: ResolvedAppearance) {
  if (typeof document === "undefined") return;
  document.documentElement.dataset.appearanceTheme = resolved;
  document.documentElement.dataset.bsTheme = resolved;
}

function publish(next: AppearanceSnapshot) {
  snapshot = Object.freeze(next);
  applyDocumentTheme(snapshot.resolved);
  subscribers.forEach((subscriber) => subscriber());
}

function isAppearancePreference(value: string | null): value is AppearancePreference {
  return value === "system" || value === "light" || value === "dark";
}

function resolveStoredPreference(value: string | null): AppearancePreference {
  if (value === null) return "system";
  if (isAppearancePreference(value)) return value;
  console.warn("Saved appearance preference is invalid; using the system preference.");
  return "system";
}

function initialize() {
  if (initialized || typeof window === "undefined") return;
  initialized = true;

  let preference: AppearancePreference = "system";
  let persistenceUnavailable = false;
  try {
    preference = resolveStoredPreference(window.localStorage.getItem(APPEARANCE_STORAGE_KEY));
  } catch {
    persistenceUnavailable = true;
    console.warn("Appearance preference could not be read from browser storage.");
  }

  snapshot = Object.freeze({
    preference,
    resolved: resolveAppearance(preference, getSystemPreference()),
    persistenceUnavailable,
  });
  applyDocumentTheme(snapshot.resolved);
}

function handleStorage(event: StorageEvent) {
  if (event.key !== APPEARANCE_STORAGE_KEY && event.key !== null) return;
  const preference = resolveStoredPreference(event.key === null ? null : event.newValue);
  publish({
    preference,
    resolved: resolveAppearance(preference, getSystemPreference()),
    persistenceUnavailable: false,
  });
}

function handleSystemPreference(event: MediaQueryListEvent) {
  if (snapshot.preference !== "system") return;
  publish({
    ...snapshot,
    resolved: resolveAppearance("system", event.matches),
  });
}

function startListening() {
  if (typeof window === "undefined") return;

  window.addEventListener("storage", handleStorage);
  removeStorageListener = () => window.removeEventListener("storage", handleStorage);

  if (typeof window.matchMedia !== "function") return;
  try {
    mediaQuery = window.matchMedia(SYSTEM_PREFERENCE_QUERY);
  } catch {
    mediaQuery = null;
    return;
  }
  if (!mediaQuery) return;

  if (typeof mediaQuery.addEventListener === "function") {
    mediaQuery.addEventListener("change", handleSystemPreference);
    removeMediaListener = () =>
      mediaQuery?.removeEventListener("change", handleSystemPreference);
  } else {
    mediaQuery.addListener(handleSystemPreference);
    removeMediaListener = () => mediaQuery?.removeListener(handleSystemPreference);
  }
}

function stopListening() {
  removeStorageListener?.();
  removeStorageListener = null;
  removeMediaListener?.();
  removeMediaListener = null;
  mediaQuery = null;
}

function subscribe(subscriber: () => void) {
  initialize();
  subscribers.add(subscriber);
  if (subscribers.size === 1) startListening();
  return () => {
    subscribers.delete(subscriber);
    if (subscribers.size === 0) stopListening();
  };
}

function getSnapshot() {
  initialize();
  return snapshot;
}

function getServerSnapshot() {
  return serverSnapshot;
}

export function initializeAppearance() {
  initialize();
}

export function useAppearance() {
  return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
}

export function setAppearancePreference(preference: AppearancePreference) {
  initialize();
  if (typeof window === "undefined") return;

  let persistenceUnavailable = false;
  try {
    if (preference === "system") {
      window.localStorage.removeItem(APPEARANCE_STORAGE_KEY);
    } else {
      window.localStorage.setItem(APPEARANCE_STORAGE_KEY, preference);
    }
  } catch {
    persistenceUnavailable = true;
    console.warn("Appearance preference could not be saved to browser storage.");
  }

  publish({
    preference,
    resolved: resolveAppearance(preference, getSystemPreference()),
    persistenceUnavailable,
  });
}

export function AppearanceProvider({ children }: { children: ReactNode }) {
  useAppearance();
  return children;
}
