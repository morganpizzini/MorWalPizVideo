import { useState } from 'react';
import { useLoaderData } from 'react-router';
import { submitAsk, type AskPublicCampaign } from '@morwalpizvideo/services';

export default function Ask() {
  const { campaign } = useLoaderData() as { campaign: AskPublicCampaign };
  const [text, setText] = useState('');
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setPending(true); setMessage(null);
    try { await submitAsk(campaign.channelName, campaign.slug, text, ''); setText(''); setMessage('Your question was received.'); }
    catch { setMessage('Unable to send your question. Please try again.'); }
    finally { setPending(false); }
  };
  return <main className="container py-5" aria-labelledby="ask-title">
    <p className="text-muted mb-2">{campaign.channelName}</p>
    <h1 id="ask-title">{campaign.title}</h1>
    <p>{campaign.description}</p>
    <form onSubmit={submit} className="mt-4" noValidate>
      <label className="form-label" htmlFor="ask-text">Your question</label>
      <textarea id="ask-text" className="form-control" maxLength={campaign.maxSubmissionLength} value={text} onChange={event => setText(event.target.value)} required rows={6} />
      <button className="btn btn-primary mt-3" disabled={pending || text.trim().length === 0}>{pending ? 'Sending...' : 'Send question'}</button>
      {message && <p className="mt-3" role="status">{message}</p>}
    </form>
  </main>;
}