import { FormEvent, useState, type ReactElement } from 'react';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { confirmNewsletter, subscribeNewsletter, unsubscribeNewsletter } from '@morwalpizvideo/services';

const configuredChannelId = (typeof window !== 'undefined'
  ? window.ENV?.VITE_MORWALPIZ_CHANNEL_ID
  : import.meta.env.VITE_MORWALPIZ_CHANNEL_ID)?.trim() ?? '';

export default function Newsletter(): ReactElement {
  const { executeRecaptcha } = useGoogleReCaptcha();
  const [email, setEmail] = useState('');
  const [language, setLanguage] = useState('IT');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const token = new URLSearchParams(typeof window === 'undefined' ? '' : window.location.search).get('token') ?? '';
  const action = new URLSearchParams(typeof window === 'undefined' ? '' : window.location.search).get('action') ?? '';

  const processToken = async (selectedAction: 'confirm' | 'unsubscribe') => {
    setError('');
    setMessage('');
    setIsSubmitting(true);
    try {
      if (!configuredChannelId || !token) throw new Error('Link newsletter non valido.');
      if (selectedAction === 'confirm') await confirmNewsletter({ channelId: configuredChannelId, token });
      else await unsubscribeNewsletter({ channelId: configuredChannelId, token });
      setMessage('Operazione completata.');
    } catch {
      setError('Impossibile completare la richiesta. Riprova dal link ricevuto.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setMessage('');
    setIsSubmitting(true);
    try {
      if (!configuredChannelId) throw new Error('Canale newsletter non configurato.');
      const recaptchaToken = executeRecaptcha ? await executeRecaptcha('newsletterSubscribe') : '';
      await subscribeNewsletter({ channelId: configuredChannelId, email, language, recaptchaToken });
      setMessage('Controlla la posta per completare la richiesta.');
    } catch {
      setError('Impossibile inviare la richiesta. Riprova.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return <main className="container py-5">
    <h1>Newsletter</h1>
    {token && (action === 'confirm' || action === 'unsubscribe') && <button className="btn btn-outline-primary mb-3" type="button" disabled={isSubmitting} onClick={() => processToken(action)}>{isSubmitting ? 'Attendi...' : action === 'confirm' ? 'Conferma iscrizione' : 'Disiscrivimi'}</button>}
    <form onSubmit={submit} className="col-md-6">
      <label className="form-label" htmlFor="newsletter-email">Email</label>
      <input id="newsletter-email" className="form-control" type="email" required value={email} onChange={event => setEmail(event.target.value)} />
      <label className="form-label mt-3" htmlFor="newsletter-language">Lingua</label>
      <select id="newsletter-language" className="form-select" value={language} onChange={event => setLanguage(event.target.value)}>
        <option value="IT">Italiano</option><option value="ENG">English</option>
      </select>
      <button className="btn btn-primary mt-3" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Invio...' : 'Iscriviti'}</button>
      {message && <p className="text-success mt-3" role="status">{message}</p>}
      {error && <p className="text-danger mt-3" role="alert">{error}</p>}
    </form>
  </main>;
}