import { useEffect, useState } from 'react';
import { Alert, Button, Card } from 'react-bootstrap';
import {
  getPushPublicKey,
  getPushSupport,
  getBackOfficePushSubscriptionSettings,
  revokeBackOfficePushSubscription,
  saveBackOfficePushSubscription,
  urlBase64ToUint8Array,
} from '@morwalpizvideo/services';

const PROMPT_KEY = 'backoffice.push.prompt';
const CREDENTIAL_KEY = 'mwp.push.credential.backoffice';
type Credential = { endpoint: string; credential: string };
function readCredential(): Credential | null {
  try { return JSON.parse(window.localStorage.getItem(CREDENTIAL_KEY) ?? 'null') as Credential | null; } catch { return null; }
}

function decided(): boolean {
  try { return window.localStorage.getItem(PROMPT_KEY) === 'dismissed'; } catch { return false; }
}
function remember(decision: 'dismissed' | 'accepted') {
  try { window.localStorage.setItem(PROMPT_KEY, decision); } catch { /* storage is optional */ }
}

export default function PushSettings(): React.ReactElement {
  const [status, setStatus] = useState<'loading' | 'off' | 'on' | 'unsupported'>('loading');
  const [credential, setCredential] = useState(readCredential);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [promptVisible, setPromptVisible] = useState(false);

  useEffect(() => {
    if (!getPushSupport().supported) { setStatus('unsupported'); return; }
    if (!credential) { setStatus('off'); setPromptVisible(!decided()); return; }
    void getBackOfficePushSubscriptionSettings(credential.endpoint, credential.credential)
      .then(() => setStatus('on'))
      .catch(() => setStatus('off'));
  }, []);

  const enable = async () => {
    setBusy(true); setError(''); setMessage('');
    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') { remember('dismissed'); setError('Consenti alle notifiche del browser per abilitarle.'); return; }
      const registration = await navigator.serviceWorker.ready;
      const key = (await getPushPublicKey()).publicKey;
      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(key) as unknown as BufferSource,
      });
      const payload = subscription.toJSON();
      const state = await saveBackOfficePushSubscription({
        endpoint: payload.endpoint ?? subscription.endpoint,
        keys: { p256dh: payload.keys?.p256dh ?? '', auth: payload.keys?.auth ?? '' },
        channelIds: [],
        applicationKey: 'backoffice',
      });
      if (state.credential) {
        window.localStorage.setItem(CREDENTIAL_KEY, JSON.stringify({ endpoint: subscription.endpoint, credential: state.credential }));
        setCredential({ endpoint: subscription.endpoint, credential: state.credential });
      }
      remember('accepted'); setStatus('on'); setMessage('Notifiche amministrative attivate.');
    } catch { setError('Impossibile attivare le notifiche. Riprova.'); }
    finally { setBusy(false); }
  };

  const disable = async () => {
    if (!credential) return;
    setBusy(true); setError(''); setMessage('');
    try {
      await revokeBackOfficePushSubscription(credential.endpoint, credential.credential);
      const subscription = await (await navigator.serviceWorker.ready).pushManager.getSubscription();
      await subscription?.unsubscribe();
      window.localStorage.removeItem(CREDENTIAL_KEY);
      setCredential(null); setStatus('off'); setMessage('Notifiche amministrative disattivate.');
    } catch { setError('Impossibile disattivare le notifiche. Riprova.'); }
    finally { setBusy(false); }
  };

  return <Card className="mt-4"><Card.Body>
    <Card.Title>Notifiche amministrative</Card.Title>
    <Card.Text>Ricevi avvisi operativi su questo browser. La richiesta del browser viene mostrata solo dopo una scelta esplicita.</Card.Text>
    {status === 'loading' && <p aria-busy="true">Caricamento…</p>}
    {status === 'unsupported' && <Alert variant="secondary">Push non disponibile in questo browser.</Alert>}
    {status === 'off' && promptVisible && <Alert variant="info">
      <strong>Vuoi ricevere gli avvisi amministrativi?</strong>
      <div className="mt-2">
        <Button size="sm" onClick={enable} disabled={busy}>Attiva notifiche</Button>{' '}
        <Button size="sm" variant="outline-secondary" onClick={() => { remember('dismissed'); setPromptVisible(false); }}>No, grazie</Button>
      </div>
    </Alert>}
    {status === 'off' && !promptVisible && <Button onClick={enable} disabled={busy}>{busy ? 'Attivazione…' : 'Attiva notifiche'}</Button>}
    {status === 'on' && <><Alert variant="success">Notifiche attive su questo browser.</Alert><Button variant="outline-danger" onClick={disable} disabled={busy}>Disattiva</Button></>}
    {message && <p className="text-success mt-3" role="status">{message}</p>}
    {error && <p className="text-danger mt-3" role="alert">{error}</p>}
    {!decided() && status === 'off' && <small className="d-block text-muted mt-2">Puoi scegliere ora o in seguito da questa pagina.</small>}
  </Card.Body></Card>;
}
