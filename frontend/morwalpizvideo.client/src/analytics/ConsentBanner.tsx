import { useEffect, useState } from 'react';
import {
  CONSENT_VERSION,
  getAnalyticsConsent,
  openConsentPreferences,
  setAnalyticsConsent,
} from './consent';
import './consent-banner.scss';

export default function ConsentBanner() {
  const [consent, setConsent] = useState(getAnalyticsConsent());
  const [open, setOpen] = useState(consent === 'unknown');
  const [preferences, setPreferences] = useState(false);

  useEffect(() => {
    const refresh = () => {
      setConsent(getAnalyticsConsent());
      setPreferences(true);
      setOpen(true);
    };
    window.addEventListener('morwalpiz-open-consent', refresh);
    return () => window.removeEventListener('morwalpiz-open-consent', refresh);
  }, []);

  if (!open) return null;
  return (
    <aside className="consent-banner" aria-label="Cookie preferences">
      <div className="consent-banner__panel">
        <div className="consent-banner__copy">
          <p className="mb-0">
            Cookie tecnici sempre attivi. Le analytics sono disattivate finché non dai il consenso.
            Versione policy {CONSENT_VERSION}.
          </p>
          {preferences && (
            <p className="consent-banner__preferences mb-0 mt-2">
              Analytics: <strong>{consent === 'accepted' ? 'attive' : 'disattivate'}</strong>. Puoi
              cambiare questa scelta in qualsiasi momento.
            </p>
          )}
        </div>
        <div className="consent-banner__actions">
          {!preferences && (
            <button
              type="button"
              className="btn consent-banner__action"
              onClick={() => setPreferences(true)}
            >
              Gestisci preferenze
            </button>
          )}
          <button
            type="button"
            className="btn consent-banner__action"
            onClick={() => {
              setAnalyticsConsent(false);
              setConsent('denied');
              setOpen(false);
            }}
          >
            Rifiuta opzionali
          </button>
          <button
            type="button"
            className="btn consent-banner__action consent-banner__action--primary"
            onClick={() => {
              setAnalyticsConsent(true);
              setConsent('accepted');
              setOpen(false);
            }}
          >
            Accetta tutti
          </button>
        </div>
      </div>
    </aside>
  );
}

export function ConsentPreferencesButton() {
  return (
    <button type="button" className="consent-preferences-button" onClick={openConsentPreferences}>
      Gestisci cookie
    </button>
  );
}
