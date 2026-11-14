import { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { Link, useNavigate, useParams } from 'react-router';
import { ApiResponseError, deleteFeedback, getFeedback, updateFeedbackStatus } from '@morwalpizvideo/services';
import type { FeedbackItem, FeedbackStatus } from '@morwalpizvideo/models';

const statuses: FeedbackStatus[] = ['New', 'Reviewing', 'Resolved', 'Dismissed'];

export default function FeedbackDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const [item, setItem] = useState<FeedbackItem>();
  const [status, setStatus] = useState<FeedbackStatus>('New');
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);
  const [loading, setLoading] = useState(true);
  const [conflict, setConflict] = useState(false);

  const load = useCallback(async () => {
    setLoading(true); setError(''); setConflict(false);
    try { const value = await getFeedback(id); setItem(value); setStatus(value.status); }
    catch { setError('Unable to load feedback.'); }
    finally { setLoading(false); }
  }, [id]);
  useEffect(() => { void load(); }, [load]);

  const save = async () => {
    if (!item) return;
    setPending(true); setError('');
    try { setItem(await updateFeedbackStatus(item.id, { status, expectedRevision: item.revision })); }
    catch (error) {
      const isConflict = error instanceof ApiResponseError && error.status === 409;
      setConflict(isConflict);
      setError(isConflict
        ? 'This feedback was changed by another administrator. Reload it before saving again.'
        : 'The feedback could not be updated. Check your connection and try again.');
    }
    finally { setPending(false); }
  };
  const remove = async () => {
    if (!item || !window.confirm('Delete this feedback permanently?')) return;
    setPending(true); setError('');
    try { await deleteFeedback(item.id); navigate('/feedback'); }
    catch { setError('The feedback could not be deleted. It may have already been removed.'); setPending(false); }
  };
  if (loading) return <p role="status">Loading feedback...</p>;
  if (error && !item) return <><Alert variant="danger">{error}</Alert><Button variant="outline-secondary" onClick={() => void load()}>Retry</Button></>;
  if (!item) return null;
  return <main><div className="d-flex justify-content-between align-items-center mb-3"><h1 className="h3">Feedback report</h1><Link to="/feedback">Back to feedback</Link></div>
    {error ? <Alert variant="danger" role="alert">{error}{conflict ? <Button variant="link" className="p-0 ms-2 align-baseline" onClick={() => void load()}>Reload feedback</Button> : null}</Alert> : null}
    <dl className="row"><dt className="col-sm-3">Reporter</dt><dd className="col-sm-9">{item.reporterUsername ?? 'Anonymous'}</dd><dt className="col-sm-3">Page</dt><dd className="col-sm-9 text-break">{item.pagePath}</dd><dt className="col-sm-3">Component</dt><dd className="col-sm-9">{item.componentKey ?? '—'}</dd><dt className="col-sm-3">Description</dt><dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>{item.description}</dd></dl>
    <div className="d-flex gap-2 align-items-center"><Form.Select aria-label="Feedback status" value={status} onChange={event => setStatus(event.target.value as FeedbackStatus)} disabled={pending}>{statuses.map(value => <option key={value}>{value}</option>)}</Form.Select><Button onClick={() => void save()} disabled={pending}>{pending ? 'Saving…' : 'Save status'}</Button><Button variant="outline-danger" onClick={() => void remove()} disabled={pending}>Delete</Button></div>
  </main>;
}
