import { useEffect, useState, type ReactElement } from 'react';
import { Link } from 'react-router';
import {
  dismissPushPrompt,
  requestPushOptIn,
  shouldShowPushPrompt,
  type PushOptInResult,
} from '@morwalpizvideo/services';
import { PUSH_APPLICATION_KEY, getPushChannelIds } from '@services/push';
import './push-opt-in.scss';

type PromptState = 'hidden' | 'visible' | 'working' | 'done' | 'failed';
type FailureReason = NonNullable<PushOptInResult['reason']>;

const failureMessages: Record<FailureReason, string> = {
  configuration: 'Il servizio notifiche non è configurato. Riprova più tardi.',
  permission: 'Il browser non ha potuto chiedere il permesso per le notifiche. Riprova.',
  'service-worker': 'Il servizio notifiche non è pronto. Ricarica la pagina e riprova.',
  subscription: 'Il browser non ha creato la sottoscrizione. Controlla i permessi e riprova.',
  persistence: 'La sottoscrizione non è stata salvata. Controlla la connessione e riprova.',
};

/**
 * One-time opt-in prompt for Web Push.
 *
 * Mounted from the root layout but rendered only after an effect confirms the browser supports push and the visitor
 * has not already decided, so it never participates in SSR and never fires the permission dialog unprompted.
 */
export default function PushOptIn(): ReactElement | null {
  const [state, setState] = useState<PromptState>('hidden');
  const [failureReason, setFailureReason] = useState<FailureReason | null>(null);

  useEffect(() => {
    if (shouldShowPushPrompt(PUSH_APPLICATION_KEY) && getPushChannelIds().length > 0) {
      setState('visible');
    }
  }, []);

  if (state === 'hidden') return null;

  const accept = async () => {
    setState('working');
    setFailureReason(null);
    try {
      const result = await requestPushOptIn({
        applicationKey: PUSH_APPLICATION_KEY,
        channelIds: getPushChannelIds(),
        language: 'IT',
      });
      setFailureReason(result.reason ?? null);
      setState(result.status === 'subscribed' ? 'done' : 'failed');
    } catch {
      // The shared helper classifies expected failures; keep the UI safe if an unexpected browser API error escapes.
      setFailureReason('subscription');
      setState('failed');
    }
  };

  const dismiss = () => {
    dismissPushPrompt(PUSH_APPLICATION_KEY);
    setState('hidden');
  };

  return (
    <aside className="push-opt-in" aria-label="Notifiche push">
      <div className="push-opt-in__panel">
        <div className="push-opt-in__copy">
          {state === 'done' ? (
            <p className="mb-0" role="status">
              Notifiche attivate. Puoi disattivarle quando vuoi dalle{' '}
              <Link to="/notifiche">impostazioni notifiche</Link>.
            </p>
          ) : (
            <>
              <p className="fw-semibold mb-1">Vuoi ricevere i nuovi video?</p>
              <p className="mb-0 fs-08 text-body-secondary">
                Ti avvisiamo con una notifica quando esce un video. Nessun account, nessuna email:
                puoi disattivarle in qualsiasi momento.
              </p>
              {state === 'failed' && (
                <p className="mb-0 mt-2 text-danger" role="alert">
                  {failureReason === undefined || failureReason === null
                    ? 'Non è stato possibile attivare le notifiche. Riprova.'
                    : failureMessages[failureReason]}
                </p>
              )}
            </>
          )}
        </div>
        <div className="push-opt-in__actions">
          {state === 'done' ? (
            <button type="button" className="btn btn-primary" onClick={() => setState('hidden')}>
              Chiudi
            </button>
          ) : (
            <>
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={dismiss}
                disabled={state === 'working'}
              >
                No, grazie
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={accept}
                disabled={state === 'working'}
              >
                {state === 'working' ? 'Attivazione…' : 'Attiva le notifiche'}
              </button>
            </>
          )}
        </div>
      </div>
    </aside>
  );
}
