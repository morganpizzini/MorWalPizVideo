import { useEffect, useState } from 'react';
import {
  getPushSupport,
  loadPushSettings,
  readStoredPushCredential,
  requestPushOptIn,
  revokePushOptIn,
  updatePushChannels,
} from '@morwalpizvideo/services';
import {
  PUSH_APPLICATION_KEY,
  loadPushChannelOptions,
  type PushChannelOption,
} from '../../services/push';

type Status = 'loading' | 'ready' | 'unsupported';

/**
 * Anonymous per-channel notification settings. Identity is the locally stored credential minted at opt-in, so the
 * page needs no account and simply disappears into an explanation when push is unavailable.
 */
export default function PushSettings() {
  const [status, setStatus] = useState<Status>('loading');
  const [options, setOptions] = useState<PushChannelOption[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [subscribed, setSubscribed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (!getPushSupport().supported) {
      setStatus('unsupported');
      return;
    }
    let cancelled = false;
    Promise.all([
      loadPushChannelOptions(),
      readStoredPushCredential() ? loadPushSettings() : Promise.resolve(null),
    ]).then(([channelOptions, state]) => {
      if (cancelled) return;
      setOptions(channelOptions);
      const active = state?.channelIds ?? [];
      setSubscribed(active.length > 0);
      setSelected(active.length > 0 ? [...active] : channelOptions.map(option => option.channelId));
      setStatus('ready');
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const toggle = (channelId: string) =>
    setSelected(current =>
      current.includes(channelId)
        ? current.filter(id => id !== channelId)
        : [...current, channelId],
    );

  const save = async () => {
    setBusy(true);
    setError('');
    setMessage('');
    const result = subscribed
      ? await updatePushChannels({
          applicationKey: PUSH_APPLICATION_KEY,
          channelIds: selected,
          language: 'IT',
        })
      : (
          await requestPushOptIn({
            applicationKey: PUSH_APPLICATION_KEY,
            channelIds: selected,
            language: 'IT',
          })
        ).state ?? null;

    if (result) {
      setSubscribed(result.channelIds.length > 0);
      setSelected([...result.channelIds]);
      setMessage('Preferenze salvate.');
    } else {
      setError('Non è stato possibile salvare le preferenze. Controlla i permessi del browser e riprova.');
    }
    setBusy(false);
  };

  const revoke = async () => {
    setBusy(true);
    setError('');
    setMessage('');
    if (await revokePushOptIn()) {
      setSubscribed(false);
      setMessage('Notifiche disattivate su questo browser.');
    } else {
      setError('Non è stato possibile disattivare le notifiche. Riprova.');
    }
    setBusy(false);
  };

  return (
    <section className="pb-push-settings">
      <h1 className="pb-push-settings__title">Notifiche</h1>
      <p className="pb-push-settings__intro">
        Scegli i canali per cui vuoi ricevere una notifica quando esce un nuovo video. Le preferenze sono anonime e
        restano su questo browser.
      </p>

      {status === 'loading' && <p className="pb-push-settings__status" aria-busy="true">Caricamento…</p>}

      {status === 'unsupported' && (
        <p className="pb-push-settings__status" role="status">
          Questo browser non supporta le notifiche push. Prova da Chrome, Edge o Firefox aggiornati.
        </p>
      )}

      {status === 'ready' && (
        <>
          {options.length === 0 ? (
            <p className="pb-push-settings__status" role="status">
              Nessun canale disponibile al momento.
            </p>
          ) : (
            <ul className="pb-push-settings__list">
              {options.map(option => (
                <li key={option.channelId} className="pb-push-settings__item">
                  <input
                    id={`push-channel-${option.channelId}`}
                    type="checkbox"
                    checked={selected.includes(option.channelId)}
                    onChange={() => toggle(option.channelId)}
                    disabled={busy}
                  />
                  <label className="pb-push-settings__item-name" htmlFor={`push-channel-${option.channelId}`}>
                    {option.channelName}
                  </label>
                </li>
              ))}
            </ul>
          )}

          <div className="pb-push-settings__actions">
            <button
              type="button"
              className="pb-push-prompt__btn pb-push-prompt__btn--primary"
              onClick={save}
              disabled={busy || selected.length === 0}
            >
              {busy ? 'Salvataggio…' : subscribed ? 'Salva preferenze' : 'Attiva le notifiche'}
            </button>
            {subscribed && (
              <button type="button" className="pb-push-prompt__btn" onClick={revoke} disabled={busy}>
                Disattiva tutto
              </button>
            )}
          </div>
        </>
      )}

      {message && <p className="pb-push-settings__success" role="status">{message}</p>}
      {error && <p className="pb-push-settings__error" role="alert">{error}</p>}
    </section>
  );
}
