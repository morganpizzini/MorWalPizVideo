import { useEffect, useState, type ReactElement } from 'react';
import type { PushSubscriptionState } from '@morwalpizvideo/models';
import {
  getPushSupport,
  loadPushSettings,
  readStoredPushCredential,
  requestPushOptIn,
  revokePushOptIn,
  updatePushChannels,
} from '@morwalpizvideo/services';
import { PUSH_APPLICATION_KEY, getPushChannelIds } from '@services/push';

type Status = 'loading' | 'subscribed' | 'unsubscribed' | 'unsupported';

/**
 * Anonymous notification settings. Identity comes entirely from the locally stored credential minted at opt-in, so
 * the page works without an account and degrades to a plain explanation when push is unavailable.
 */
export default function PushSettings(): ReactElement {
  const [status, setStatus] = useState<Status>('loading');
  const [subscription, setSubscription] = useState<PushSubscriptionState | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const channels = getPushChannelIds();

  useEffect(() => {
    if (!getPushSupport().supported) {
      setStatus('unsupported');
      return;
    }
    if (!readStoredPushCredential()) {
      setStatus('unsubscribed');
      return;
    }
    loadPushSettings().then(state => {
      setSubscription(state);
      setSelected(state ? [...state.channelIds] : []);
      setStatus(state && state.channelIds.length > 0 ? 'subscribed' : 'unsubscribed');
    });
  }, []);

  const enable = async () => {
    setBusy(true);
    setError('');
    setMessage('');
    const result = await requestPushOptIn({
      applicationKey: PUSH_APPLICATION_KEY,
      channelIds: selected.length ? selected : channels,
      language: 'IT',
    });
    if (result.status === 'subscribed') {
      setSubscription(result.state ?? null);
      setStatus('subscribed');
      setMessage('Notifiche attivate su questo browser.');
    } else if (result.status === 'denied') {
      setError('Il browser ha bloccato le notifiche. Sbloccale dalle impostazioni del sito e riprova.');
    } else {
      setError('Le notifiche non sono disponibili in questo momento. Riprova più tardi.');
    }
    setBusy(false);
  };

  const saveChannels = async () => {
    if (!selected.length) { await disable(); return; }
    setBusy(true); setError(''); setMessage('');
    const result = await updatePushChannels({ applicationKey: PUSH_APPLICATION_KEY, channelIds: selected, language: 'IT' });
    if (result) { setSubscription(result); setStatus('subscribed'); setMessage('Preferenze salvate.'); }
    else setError('Non è stato possibile salvare le preferenze.');
    setBusy(false);
  };

  const disable = async () => {
    setBusy(true);
    setError('');
    setMessage('');
    const revoked = await revokePushOptIn();
    if (revoked) {
      setSubscription(null);
      setStatus('unsubscribed');
      setMessage('Notifiche disattivate. Non riceverai più avvisi su questo browser.');
    } else {
      setError('Non è stato possibile disattivare le notifiche. Riprova.');
    }
    setBusy(false);
  };

  return (
    <main className="container py-5">
      <h1>Notifiche</h1>
      <p className="col-md-8">
        Le notifiche ti avvisano quando pubblico un nuovo video. Sono anonime: restano legate a questo browser e non
        richiedono né account né email.
      </p>

      {status === 'loading' && (
        <p className="placeholder-glow" aria-busy="true">
          <span className="placeholder col-4" />
        </p>
      )}

      {status === 'unsupported' && (
        <p className="text-body-secondary" role="status">
          Questo browser non supporta le notifiche push. Prova da Chrome, Edge o Firefox aggiornati.
        </p>
      )}

      {status === 'unsubscribed' && (
        <button type="button" className="btn btn-primary" onClick={enable} disabled={busy}>
          {busy ? 'Attivazione…' : 'Attiva le notifiche'}
        </button>
      )}

      {status === 'subscribed' && (
        <>
          <p className="mb-3" role="status">
            Notifiche attive per {subscription?.channelIds.length ?? 0} canale/i su questo browser.
          </p>
          {channels.length > 1 && <fieldset className="mb-3"><legend className="h6">Canali</legend>{channels.map(channel => <label className="d-block" key={channel}><input type="checkbox" checked={selected.includes(channel)} onChange={() => setSelected(current => current.includes(channel) ? current.filter(id => id !== channel) : [...current, channel])} /> <span className="ms-2">{channel}</span></label>)}<button type="button" className="btn btn-primary mt-2" onClick={saveChannels} disabled={busy}>Salva canali</button></fieldset>}
          <button type="button" className="btn btn-outline-danger" onClick={disable} disabled={busy}>
            {busy ? 'Disattivazione…' : 'Disattiva le notifiche'}
          </button>
        </>
      )}

      {message && <p className="text-success mt-3" role="status">{message}</p>}
      {error && <p className="text-danger mt-3" role="alert">{error}</p>}
    </main>
  );
}
