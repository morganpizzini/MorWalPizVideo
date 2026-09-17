import React from 'react';
import { useLoaderData, useFetcher, Link } from 'react-router';
import { Form, Table } from 'react-bootstrap';
import { fetchAskCampaigns, createAskCampaign, updateAskCampaign, publishAskToTelegram } from '@morwalpizvideo/services';
import type { AskCampaign, AskCampaignRequest } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import { useToast } from '@components/ToastNotification/ToastContext';
import { Send } from 'lucide-react';

export async function Loader(): Promise<AskCampaign[]> { return fetchAskCampaigns(); }
export async function Action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  if (values.action === 'publishTelegram') {
    try { await publishAskToTelegram(String(values.id)); return { success: true, action: 'publishTelegram' }; }
    catch { return { errors: ['Unable to publish campaign to Telegram.'] }; }
  }
  const policy = { maxSubmissionLength: Number(values.maxSubmissionLength || 2000), retentionDays: Number(values.retentionDays || 90), rateLimitPerHour: Number(values.rateLimitPerHour || 5), duplicateWindowMinutes: Number(values.duplicateWindowMinutes || 60), moderationMode: 0, recaptchaRequired: values.recaptchaRequired === 'on', allowNamedSubmissions: values.allowNamedSubmissions === 'on', nameRequired: values.nameRequired === 'on' };
  const payload: AskCampaignRequest = { title: String(values.title), description: String(values.description), slug: String(values.slug), status: String(values.status) as AskCampaignRequest['status'], policy, startAt: String(values.startAt || '') || undefined, endAt: String(values.endAt || '') || undefined };
  try { if (values.id) await updateAskCampaign(String(values.id), payload); else await createAskCampaign(payload); return { success: true }; } catch { return { errors: ['Unable to save campaign.'] }; }
}
export function Component() {
  const campaigns = useLoaderData() as AskCampaign[];
  const fetcher = useFetcher();
  const publishFetcher = useFetcher();
  const toast = useToast();
  const [search, setSearch] = React.useState('');
  React.useEffect(() => {
    if (!publishFetcher.data) return;
    if (publishFetcher.data.success) toast.show('Published', 'Campaign published to Telegram.', { variant: 'success' });
    else if (publishFetcher.data.errors) toast.show('Telegram publish failed', publishFetcher.data.errors.join(' '), { variant: 'danger' });
  }, [publishFetcher.data, toast]);
  const filtered = campaigns.filter(campaign => `${campaign.title} ${campaign.slug}`.toLowerCase().includes(search.toLowerCase()));
  return <><PageHeader title="Ask campaigns" /><div className="d-flex gap-2 mb-3"><Form.Control placeholder="Search campaigns" value={search} onChange={event => setSearch(event.target.value)} /><Link className="btn btn-primary" to="create">Create campaign</Link></div><Table striped responsive><thead><tr><th>Title</th><th>Slug</th><th>Status</th><th>Submissions</th><th /></tr></thead><tbody>{filtered.map(campaign => <tr key={campaign.id}><td>{campaign.title}</td><td>{campaign.slug}</td><td>{String(campaign.status)}</td><td>{campaign.submissionCount}</td><td><Link to={campaign.id}>Submissions</Link> <Link to={`${campaign.id}/edit`}>Edit</Link> <publishFetcher.Form method="post" className="d-inline"><input type="hidden" name="action" value="publishTelegram" /><input type="hidden" name="id" value={campaign.id} /><button type="submit" className="btn btn-link p-0 align-baseline" disabled={publishFetcher.state !== 'idle'} title="Publish to Telegram" aria-label={`Publish ${campaign.title} to Telegram`}><Send size={16} aria-hidden="true" /></button></publishFetcher.Form></td></tr>)}</tbody></Table><span>{fetcher.data?.errors?.join(' ')}</span></>;
}
export default Component;
