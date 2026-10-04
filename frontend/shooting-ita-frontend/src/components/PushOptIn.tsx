import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  dismissPushPrompt,
  requestPushOptIn,
  shouldShowPushPrompt,
} from '@morwalpizvideo/services';
import { PUSH_APPLICATION_KEY, loadPushChannelOptions } from '../services/push';

type PromptState = 'hidden' | 'visible' | 'working' | 'done' | 'failed';

/**
 * One-time Web Push opt-in. Rendered only from an effect so it never participates in SSR and never triggers the
 * browser permission dialog without an explicit click.
 */
export default function PushOptIn() {
  const [state, setState] = useState<PromptState>('hidden');
  const [channelIds, setChannelIds] = useState<string[]>([]);

  useEffect(() => {
    if ((import.meta.env.PROD && !import.meta.env.VITE_SHOOTING_ITA_PUSH_ORIGIN) || !shouldShowPushPrompt(PUSH_APPLICATION_KEY)) return;
    let cancelled = false;
    loadPushChannelOptions()
      .then(options => {
        if (cancelled || options.length === 0) return;
        setChannelIds(options.map(option => option.channelId));
        setState('visible');
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, []);

  if (state === 'hidden') return null;

  const accept = async () => {
    setState('working');
    const result = await requestPushOptIn({
      applicationKey: PUSH_APPLICATION_KEY,
      channelIds,
      language: 'IT',
    });
    setState(result.status === 'subscribed' ? 'done' : 'failed');
  };

  const dismiss = () => {
    dismissPushPrompt(PUSH_APPLICATION_KEY);
    setState('hidden');
  };

  return (
    <aside className="pb-push-prompt" aria-label="Notifiche push">
      <div className="pb-push-prompt__panel">
        <div className="pb-push-prompt__copy">
          {state === 'done' ? (
            <p className="pb-push-prompt__lead" role="status">
              Notifiche attivate. Gestiscile dalle{' '}
              <Link to="/notifiche">impostazioni notifiche</Link>.
            </p>
          ) : (
            <>
              <p className="pb-push-prompt__lead">Resta aggiornato sui nuovi video</p>
              <p className="pb-push-prompt__detail">
                Una notifica quando esce un video dei canali Shooting ITA. Niente account, niente email.
              </p>
              {state === 'failed' && (
                <p className="pb-push-prompt__error" role="alert">
                  Non è stato possibile attivare le notifiche. Controlla i permessi del browser e riprova.
                </p>
              )}
            </>
          )}
        </div>
        <div className="pb-push-prompt__actions">
          {state === 'done' ? (
            <button
              type="button"
              className="pb-push-prompt__btn pb-push-prompt__btn--primary"
              onClick={() => setState('hidden')}
            >
              Chiudi
            </button>
          ) : (
            <>
              <button
                type="button"
                className="pb-push-prompt__btn"
                onClick={dismiss}
                disabled={state === 'working'}
              >
                No, grazie
              </button>
              <button
                type="button"
                className="pb-push-prompt__btn pb-push-prompt__btn--primary"
                onClick={accept}
                disabled={state === 'working'}
              >
                {state === 'working' ? 'Attivazione…' : 'Attiva'}
              </button>
            </>
          )}
        </div>
      </div>
    </aside>
  );
}
