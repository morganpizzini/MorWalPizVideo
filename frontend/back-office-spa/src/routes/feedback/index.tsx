import { Alert, Form, Table } from 'react-bootstrap';
import { Link, useLoaderData, useNavigate, useNavigation, useRevalidator } from 'react-router';
import { getFeedbackPage } from '@morwalpizvideo/services';
import type { FeedbackPage, FeedbackStatus } from '@morwalpizvideo/models';

const statuses: FeedbackStatus[] = ['New', 'Reviewing', 'Resolved', 'Dismissed'];

export async function loader({ request }: { request: Request }) {
  const url = new URL(request.url);
  const page = Math.max(1, Number(url.searchParams.get('page') ?? 1));
  const pageSize = Math.min(50, Math.max(1, Number(url.searchParams.get('pageSize') ?? 20)));
  if (!Number.isInteger(page) || !Number.isInteger(pageSize)) throw new Response('Invalid pagination', { status: 400 });
  return getFeedbackPage(
    (url.searchParams.get('status') as FeedbackStatus | null) || undefined,
    url.searchParams.get('application') || undefined,
    page,
    pageSize,
  );
}

export default function FeedbackPageView() {
  const page = useLoaderData() as FeedbackPage;
  const navigate = useNavigate();
  const navigation = useNavigation();
  const revalidator = useRevalidator();
  const params = new URLSearchParams(location.search);
  const status = params.get('status') ?? '';
  const application = params.get('application') ?? '';
  const pending = navigation.state !== 'idle' || revalidator.state !== 'idle';
  const updateQuery = (key: string, value: string) => {
    const next = new URLSearchParams(location.search);
    if (value) next.set(key, value); else next.delete(key);
    if (key !== 'page') next.delete('page');
    navigate(`?${next.toString()}`);
  };
  return <main>
    <div className="d-flex justify-content-between align-items-center mb-3">
      <div><h1 className="h3 mb-1">Feedback</h1><p className="text-secondary mb-0">Global reports from the public applications.</p></div>
      <div className="d-flex gap-2">
        <Form.Select aria-label="Filter by application" value={application} onChange={event => updateQuery('application', event.target.value)}>
          <option value="">All applications</option><option value="Workshop">Workshop</option>
        </Form.Select>
        <Form.Select aria-label="Filter by status" value={status} onChange={event => updateQuery('status', event.target.value)}>
          <option value="">All statuses</option>{statuses.map(value => <option key={value} value={value}>{value}</option>)}
        </Form.Select>
      </div>
    </div>
    {pending ? <p role="status">Loading feedback...</p> : null}
    {!pending && page.items.length === 0 ? <p>No feedback found.</p> :
      <Table responsive hover className="align-middle"><thead><tr><th>Status</th><th>Description</th><th>Reporter</th><th>Application</th><th>Received</th><th /></tr></thead>
        <tbody>{page.items.map(item => <tr key={item.id}><td>{item.status}</td><td>{item.description.slice(0, 120)}</td><td>{item.reporterUsername ?? 'Anonymous'}</td><td>{item.application}</td><td>{new Date(item.createdAtUtc).toLocaleString()}</td><td><Link className="btn btn-sm btn-outline-primary" to={`/feedback/${item.id}`}>Open</Link></td></tr>)}</tbody>
      </Table>}
    <nav aria-label="Feedback pages" className="d-flex justify-content-between">
      {page.page > 1 ? <button className="btn btn-link" onClick={() => updateQuery('page', String(page.page - 1))}>Previous</button> : <span />}
      <span className="text-secondary">Page {page.page} of {Math.max(1, Math.ceil(page.total / page.pageSize))}</span>
      {page.page * page.pageSize < page.total ? <button className="btn btn-link" onClick={() => updateQuery('page', String(page.page + 1))}>Next</button> : <span />}
    </nav>
    {!pending && <Alert variant="light" className="mt-3"><button className="btn btn-sm btn-outline-secondary" onClick={() => revalidator.revalidate()}>Retry latest results</button></Alert>}
  </main>;
}
