import { useEffect, useState } from 'react';
import {
  CONSENT_VERSION,
  getAnalyticsConsent,
  openConsentPreferences,
  setAnalyticsConsent,
} from './consent';

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
    <aside
      className="position-fixed bottom-0 start-0 end-0 bg-dark text-white p-3 shadow-lg"
      style={{ zIndex: 1100 }}
      aria-label="Cookie preferences"
    >
      <div className="container d-flex flex-column flex-md-row align-items-md-center gap-3">
        <div className="flex-grow-1">
          <p className="mb-0">
            Cookie tecnici sempre attivi. Le analytics sono disattivate finché non dai il consenso.
            Versione policy {CONSENT_VERSION}.
          </p>
          {preferences && (
            <p className="small mb-0 mt-2">
              Analytics: <strong>{consent === 'accepted' ? 'attive' : 'disattivate'}</strong>. Puoi
              cambiare questa scelta in qualsiasi momento.
            </p>
          )}
        </div>
        <div className="d-flex gap-2 flex-wrap">
          {!preferences && (
            <button
              type="button"
              className="btn btn-outline-light"
              onClick={() => setPreferences(true)}
            >
              Gestisci preferenze
            </button>
          )}
          <button
            type="button"
            className="btn btn-outline-light"
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
            className="btn btn-primary"
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
    <button type="button" className="btn btn-link p-0 link-light" onClick={openConsentPreferences}>
      Gestisci cookie
    </button>
  );
}
