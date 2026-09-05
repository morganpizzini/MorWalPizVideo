import { useEffect, useState } from 'react';
import { useLoaderData } from 'react-router';
import PageHeader from '@components/PageHeader';
import { changeNewsletterState, createNewsletter, fetchNewsletterStats, fetchNewsletterSubscribers, fetchNewsletterTemplates, getNewsletter, previewNewsletter, sendNewsletter, updateNewsletter } from '@morwalpizvideo/services';

interface NewsletterRow { id: string; name: string; state: string; subjectIt: string; subjectEng: string; }

export default function Newsletters(): React.ReactElement {
  const newsletters = useLoaderData() as NewsletterRow[];
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

  useEffect(() => { void fetchNewsletterTemplates().then(value => setTemplates(Array.isArray(value) ? value : [])); }, []);
  const select = async (id: string) => {
    setSelectedId(id);
    const value = await getNewsletter(id) as Record<string, unknown>;
    setDetail(value);
    setName(String(value.name ?? '')); setSubjectIt(String(value.subjectIt ?? '')); setSubjectEng(String(value.subjectEng ?? ''));
    setTemplateId(String(value.templateId ?? '')); setTemplateVersion(Number(value.templateVersion ?? 1));
  };
  const save = async () => {
    const request = { name, subjectIt, subjectEng, templateId, templateVersion, sections: [{ type: 'text', title: null, body: '', imageUrl: null, shortLinkCode: null }] };
    if (selectedId) setDetail(await updateNewsletter(selectedId, request) as Record<string, unknown>);
    else setDetail(await createNewsletter(request) as Record<string, unknown>);
  };
  const refreshDetail = async () => {
    if (!selectedId) return;
    setPreview(await previewNewsletter(selectedId) as Record<string, unknown>);
    setStats(await fetchNewsletterStats(selectedId) as Record<string, unknown>);
    setSubscribers(await fetchNewsletterSubscribers(selectedId) as Array<Record<string, unknown>>);
  };
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
      <div className="d-flex gap-2 mt-3"><button type="button" className="btn btn-primary" onClick={() => void save()}>Save</button>{selectedId && <><button type="button" className="btn btn-outline-secondary" onClick={() => void refreshDetail()}>Preview / stats</button><button type="button" className="btn btn-outline-success" onClick={() => void changeNewsletterState(selectedId, 'Approved')}>Approve</button><button type="button" className="btn btn-success" onClick={() => void sendNewsletter(selectedId)}>Send</button></>}</div>
      {detail && <p className="mt-3 mb-0">State: {String(detail.state ?? '')}</p>}
      {preview && <div className="mt-3" dangerouslySetInnerHTML={{ __html: String(preview.html ?? '') }} />}
      {stats && <pre className="mt-3">{JSON.stringify(stats, null, 2)}</pre>}
      {subscribers.length > 0 && <p className="mt-3">Subscribers: {subscribers.length}</p>}
    </section>
  </>;
}