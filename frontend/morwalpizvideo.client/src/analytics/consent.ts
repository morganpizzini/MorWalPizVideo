import ReactGA from 'react-ga4';

export const CONSENT_VERSION = '2026-11-01';
const STORAGE_KEY = 'morwalpiz-consent';
type ConsentState = 'unknown' | 'denied' | 'accepted';

let state: ConsentState = 'unknown';
let initialized = false;

export function getAnalyticsConsent(): ConsentState {
  if (typeof window === 'undefined') return 'unknown';
  if (state !== 'unknown') return state;
  try {
    const stored = JSON.parse(window.localStorage.getItem(STORAGE_KEY) ?? 'null') as {
      version?: string;
      analytics?: boolean;
    } | null;
    state =
      stored?.version === CONSENT_VERSION ? (stored.analytics ? 'accepted' : 'denied') : 'unknown';
  } catch {
    state = 'unknown';
  }
  return state;
}

export function setAnalyticsConsent(accepted: boolean): void {
  state = accepted ? 'accepted' : 'denied';
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ version: CONSENT_VERSION, analytics: accepted })
    );
    if (!accepted) {
      for (const cookieName of ['_ga', '_gid', '_gat', 'AMP_TOKEN']) {
        document.cookie = `${cookieName}=; Max-Age=0; path=/`;
      }
    }
    if (accepted && !initialized && window.location.hostname !== 'localhost') {
      ReactGA.initialize(import.meta.env.VITE_GA_MEASUREMENT_ID ?? 'G-ST9GQYL925');
      initialized = true;
    }
    window.dispatchEvent(new CustomEvent('morwalpiz-consent-changed'));
  }
}

export function trackPageView(pageType: string, routeTemplate: string): void {
  if (getAnalyticsConsent() !== 'accepted') return;
  ReactGA.send({ hitType: 'pageview', page: routeTemplate, title: pageType });
}

export function trackEvent(
  name: string,
  parameters: Record<string, string | number | undefined> = {}
): void {
  if (getAnalyticsConsent() !== 'accepted') return;
  ReactGA.event(
    name,
    Object.fromEntries(Object.entries(parameters).filter(([, value]) => value !== undefined))
  );
}

export function openConsentPreferences(): void {
  if (typeof window !== 'undefined')
    window.dispatchEvent(new CustomEvent('morwalpiz-open-consent'));
}
