import { useEffect, useState } from 'react';
import { useLoaderData, useRevalidator } from 'react-router';
import PageHeader from '@components/PageHeader';
import { changeNewsletterState, createNewsletter, fetchNewsletterStats, fetchNewsletterSubscribers, fetchNewsletterTemplates, getNewsletter, previewNewsletter, scheduleNewsletter, sendNewsletter, updateNewsletter } from '@morwalpizvideo/services';

interface NewsletterRow { id: string; name: string; state: string; subjectIt: string; subjectEng: string; }

const toUtcIso = (value: string): string => value ? new Date(`${value}:00Z`).toISOString() : '';

export default function Newsletters(): React.ReactElement {
  const newsletters = useLoaderData() as NewsletterRow[];
  const revalidator = useRevalidator();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<Record<string, unknown> | null>(null);
  const [preview, setPreview] = useState<Record<string, unknown> | null>(null);
  const [stats, setStats] = useState<Record<string, unknown> | null>(null);
  const [subscribers, setSubscribers] = useState<Array<Record<string, unknown>>>([]);
  const [templates, setTemplates] = useState<unknown[]>([]);
  const [name, setName] = useState('');
  const [subjectIt, setSubjectIt] = useState('');
  const [subjectEng, setSubjectEng] = useState('');
  const [templateId, setTemplateId] = useState('');
  const [templateVersion, setTemplateVersion] = useState(1);
  const [deliveryMode, setDeliveryMode] = useState('Draft');
  const [scheduledAtUtc, setScheduledAtUtc] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => { void fetchNewsletterTemplates().then(value => setTemplates(Array.isArray(value) ? value : [])); }, []);
  const select = async (id: string) => {
    setSelectedId(id);
    const value = await getNewsletter(id) as Record<string, unknown>;
    setDetail(value); setError(null);
    setName(String(value.name ?? '')); setSubjectIt(String(value.subjectIt ?? '')); setSubjectEng(String(value.subjectEng ?? ''));
    setTemplateId(String(value.templateId ?? '')); setTemplateVersion(Number(value.templateVersion ?? 1));
    setScheduledAtUtc(value.scheduledAtUtc ? String(value.scheduledAtUtc).slice(0, 16) : '');
  };
  const run = async (operation: () => Promise<unknown>) => {
    setBusy(true); setError(null);
    try { await operation(); revalidator.revalidate(); }
    catch (value) { setError(value instanceof Error ? value.message : 'Newsletter operation failed.'); }
    finally { setBusy(false); }
  };
  const save = async () => {
    const request = { name, subjectIt, subjectEng, templateId, templateVersion, deliveryMode, scheduledAtUtc: deliveryMode === 'Scheduled' ? toUtcIso(scheduledAtUtc) : null, sections: [{ type: 'text', title: null, body: '', imageUrl: null, shortLinkCode: null }] };
    await run(async () => setDetail((selectedId ? await updateNewsletter(selectedId, request) : await createNewsletter(request)) as Record<string, unknown>));
  };
  const refreshDetail = async () => {
    if (!selectedId) return;
    setPreview(await previewNewsletter(selectedId) as Record<string, unknown>);
    setStats(await fetchNewsletterStats(selectedId) as Record<string, unknown>);
    setSubscribers(await fetchNewsletterSubscribers(selectedId) as Array<Record<string, unknown>>);
  };
  const schedule = async () => {
    if (!selectedId) return;
    await run(async () => setDetail(await scheduleNewsletter(selectedId, toUtcIso(scheduledAtUtc)) as Record<string, unknown>));
  };
  const canSchedule = !['Sending', 'Sent', 'Cancelled'].includes(String(detail?.state ?? ''));
  return <>
    <PageHeader title="Newsletters" />
    <div className="table-responsive"><table className="table">
      <thead><tr><th>Name</th><th>State</th><th>Italian subject</th><th>English subject</th></tr></thead>
      <tbody>{newsletters.map(item => <tr key={item.id}><td><button type="button" className="btn btn-link p-0" onClick={() => void select(item.id)}>{item.name}</button></td><td>{item.state}</td><td>{item.subjectIt}</td><td>{item.subjectEng}</td></tr>)}</tbody>
    </table></div>
    <section className="border rounded p-3 mt-4" aria-label="Newsletter editor">
      <h2 className="h5">{selectedId ? 'Edit newsletter' : 'Create newsletter'}</h2>
      <div className="row g-2">
        <div className="col-md-3"><label className="form-label" htmlFor="newsletter-name">Name</label><input id="newsletter-name" className="form-control" value={name} onChange={event => setName(event.target.value)} /></div>
        <div className="col-md-3"><label className="form-label" htmlFor="newsletter-it">Italian subject</label><input id="newsletter-it" className="form-control" value={subjectIt} onChange={event => setSubjectIt(event.target.value)} /></div>
        <div className="col-md-3"><label className="form-label" htmlFor="newsletter-eng">English subject</label><input id="newsletter-eng" className="form-control" value={subjectEng} onChange={event => setSubjectEng(event.target.value)} /></div>
        <div className="col-md-2"><label className="form-label" htmlFor="newsletter-template">Template</label><select id="newsletter-template" className="form-select" value={templateId} onChange={event => setTemplateId(event.target.value)}><option value="">Select</option>{templates.map(template => { const item = template as { id?: string; name?: string; version?: number }; return <option key={item.id} value={item.id}>{item.name} v{item.version}</option>; })}</select></div>
        <div className="col-md-1"><label className="form-label" htmlFor="newsletter-version">Version</label><input id="newsletter-version" type="number" className="form-control" value={templateVersion} onChange={event => setTemplateVersion(Number(event.target.value))} /></div>
      </div>
      <div className="row g-2 mt-2">
        <div className="col-md-3"><label className="form-label" htmlFor="newsletter-delivery">Delivery</label><select id="newsletter-delivery" className="form-select" value={deliveryMode} onChange={event => setDeliveryMode(event.target.value)}><option>Draft</option><option>Immediate</option><option>Scheduled</option></select></div>
        {(deliveryMode === 'Scheduled' || detail?.state === 'Scheduled') && <div className="col-md-4"><label className="form-label" htmlFor="newsletter-schedule">Send at (UTC)</label><input id="newsletter-schedule" type="datetime-local" step={1800} className="form-control" value={scheduledAtUtc} onChange={event => setScheduledAtUtc(event.target.value)} /></div>}
      </div>
      <div className="d-flex gap-2 mt-3"><button type="button" className="btn btn-primary" disabled={busy} onClick={() => void save()}>Save</button>{selectedId && <><button type="button" className="btn btn-outline-secondary" disabled={busy} onClick={() => void refreshDetail()}>Preview / stats</button>{canSchedule && <button type="button" className="btn btn-outline-primary" disabled={busy || !scheduledAtUtc} onClick={() => void schedule()}>Schedule</button>}<button type="button" className="btn btn-outline-success" disabled={busy} onClick={() => void run(() => changeNewsletterState(selectedId, 'Approved'))}>Approve</button><button type="button" className="btn btn-success" disabled={busy} onClick={() => void run(() => sendNewsletter(selectedId))}>Send now</button></>}</div>
      {error && <div className="alert alert-danger mt-3" role="alert">{error}</div>}
      {busy && <p className="mt-3" role="status">Saving newsletter...</p>}
      {detail && <p className="mt-3 mb-0">State: {String(detail.state ?? '')}</p>}
      {preview && <div className="mt-3" dangerouslySetInnerHTML={{ __html: String(preview.html ?? '') }} />}
      {stats && <pre className="mt-3">{JSON.stringify(stats, null, 2)}</pre>}
      {subscribers.length > 0 && <p className="mt-3">Subscribers: {subscribers.length}</p>}
    </section>
  </>;
}