import { useEffect, useState } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData, useNavigate } from 'react-router';
import type { FeedbackItem, FeedbackStatus } from '@morwalpizvideo/models';

const statuses: FeedbackStatus[] = ['New', 'Reviewing', 'Resolved', 'Dismissed'];

type ActionData = {
  item?: FeedbackItem;
  deleted?: boolean;
  conflict?: boolean;
  error?: string;
};

export default function FeedbackDetailPage() {
  const loadedItem = useLoaderData() as FeedbackItem;
  const fetcher = useFetcher<ActionData>();
  const navigate = useNavigate();
  const [item, setItem] = useState(loadedItem);
  const [status, setStatus] = useState<FeedbackStatus>(loadedItem.status);
  const pending = fetcher.state !== 'idle';
  const result = fetcher.data;

  useEffect(() => {
    if (result?.item) {
      setItem(result.item);
      setStatus(result.item.status);
    } else if (result?.deleted) {
      navigate('/feedback');
    }
  }, [navigate, result]);

  const submit = (intent: 'save' | 'delete') => {
    if (intent === 'delete' && !window.confirm('Delete this feedback permanently?')) return;
    fetcher.submit(
      { intent, id: item.id, status, expectedRevision: String(item.revision) },
      { method: 'post' },
    );
  };

  return <main><div className="d-flex justify-content-between align-items-center mb-3"><h1 className="h3">Feedback report</h1><Link to="/feedback">Back to feedback</Link></div>
    {result?.error ? <Alert variant="danger" role="alert">{result.error}{result.conflict ? <Button variant="link" className="p-0 ms-2 align-baseline" onClick={() => navigate('.', { replace: true })}>Reload feedback</Button> : null}</Alert> : null}
    <dl className="row"><dt className="col-sm-3">Reporter</dt><dd className="col-sm-9">{item.reporterUsername ?? 'Anonymous'}</dd><dt className="col-sm-3">Page</dt><dd className="col-sm-9 text-break">{item.pagePath}</dd><dt className="col-sm-3">Component</dt><dd className="col-sm-9">{item.componentKey ?? '—'}</dd><dt className="col-sm-3">Description</dt><dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>{item.description}</dd></dl>
    <div className="d-flex gap-2 align-items-center"><Form.Select aria-label="Feedback status" value={status} onChange={event => setStatus(event.target.value as FeedbackStatus)} disabled={pending}>{statuses.map(value => <option key={value}>{value}</option>)}</Form.Select><Button onClick={() => submit('save')} disabled={pending}>{pending ? 'Saving…' : 'Save status'}</Button><Button variant="outline-danger" onClick={() => submit('delete')} disabled={pending}>Delete</Button></div>
  </main>;
}
