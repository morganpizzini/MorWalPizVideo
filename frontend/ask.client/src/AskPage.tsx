import { useState } from 'react';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { useLoaderData } from 'react-router';
import { useParams } from 'react-router';
import { reactToAskSubmission, submitAsk, type AskPublicCampaign } from '@morwalpizvideo/services';

export default function AskPage() {
  const { campaign } = useLoaderData() as { campaign: AskPublicCampaign };
  const { channelName, campaignSlug } = useParams();
  const { executeRecaptcha } = useGoogleReCaptcha();
  const [text, setText] = useState('');
  const [name, setName] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [questions, setQuestions] = useState(campaign.questions ?? []);
  const isNamed = campaign.allowNamedSubmissions && (campaign.nameRequired || name.trim().length > 0);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); setError(null); setSent(false);
    if (!text.trim() || text.length > campaign.maxSubmissionLength || (campaign.nameRequired && !name.trim())) {
      setError('Check the required fields and message length.'); return;
    }
    setPending(true);
    try {
      const recaptchaToken = campaign.recaptchaRequired ? (executeRecaptcha ? await executeRecaptcha('ask_submit') : '') : '';
      if (campaign.recaptchaRequired && !recaptchaToken) throw new Error('captcha');
      await submitAsk(campaign.channelName, campaign.slug, text, recaptchaToken, name.trim() || undefined);
      setText(''); setName(''); setSent(true);
    } catch (cause) {
      setError(cause instanceof Error && cause.message === 'captcha' ? 'Captcha verification is unavailable. Please try again.' : 'Unable to send your question. Please try again later.');
    } finally { setPending(false); }
  };
  return <main className="ask-shell" aria-labelledby="ask-title">
    <section className="ask-panel">
      <p className="ask-kicker">{campaign.channelName}</p>
      <h1 id="ask-title">{campaign.title}</h1>
      <p className="ask-description">{campaign.description}</p>
      <form onSubmit={submit} noValidate>
        {campaign.allowNamedSubmissions && <label>Name (optional){campaign.nameRequired ? ' *' : ''}<input value={name} maxLength={120} onChange={event => setName(event.target.value)} disabled={pending} required={campaign.nameRequired} /></label>}
        <label>Your question<textarea value={text} maxLength={campaign.maxSubmissionLength} onChange={event => setText(event.target.value)} disabled={pending} required rows={7} /></label>
        <div className="ask-footer"><span>{text.length}/{campaign.maxSubmissionLength}</span><button type="submit" disabled={pending || !text.trim()}>{pending ? 'Sending...' : 'Send question'}</button></div>
        {error && <p role="alert" className="ask-error">{error}</p>}
        {sent && <p role="status" className="ask-success">Your question was received.</p>}
        <p className="ask-privacy">{isNamed ? 'Your name will be shown with your question.' : 'You can submit anonymously.'}</p>
      </form>
      {questions.length > 0 && <section className="ask-questions" aria-label="Answered questions"><h2>Answered questions</h2>{questions.map(question => <article key={question.id}><p>{question.text}</p>{question.response && <p><strong>{question.response.author}:</strong> {question.response.content}</p>}<button type="button" onClick={async () => { if (!channelName || !campaignSlug) return; const result = await reactToAskSubmission(channelName, campaignSlug, question.id); if (result.accepted) setQuestions(current => current.map(item => item.id === question.id ? { ...item, reactionCount: result.count } : item)); }}>{question.reactionCount} helpful</button></article>)}</section>}
    </section>
  </main>;
}
