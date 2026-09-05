import React from 'react';
import { Button, Table } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData } from 'react-router';
import { fetchFaqCandidates, generateFaqCandidates, reviewFaqCandidate } from '@morwalpizvideo/services';
import type { FaqCandidateAdmin } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

export async function Loader(): Promise<FaqCandidateAdmin[]> { return fetchFaqCandidates(0); }
export async function Action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  try { if (values.intent === 'generate') await generateFaqCandidates(String(values.campaignIds || '').split(',').map(value => value.trim()).filter(Boolean)); else await reviewFaqCandidate(String(values.id), Number(values.status)); return { success: true }; }
  catch (error) { return { errors: [error instanceof Error ? error.message : 'Unable to process candidate.'] }; }
}
export function Component(): React.ReactElement {
  const candidates = useLoaderData() as FaqCandidateAdmin[];
  const fetcher = useFetcher();
  return <><PageHeader title="FAQ candidates" /><Link className="btn btn-secondary mb-3" to="/faq">Back to FAQs</Link><fetcher.Form method="post" className="d-flex gap-2 mb-3"><input type="hidden" name="intent" value="generate" /><input className="form-control" name="campaignIds" placeholder="Campaign IDs, comma separated" /><Button type="submit">Generate candidates</Button></fetcher.Form>{fetcher.data?.errors && <div className="alert alert-warning">{fetcher.data.errors.join(' ')}</div>}<Table striped responsive><thead><tr><th>Question</th><th>Answer</th><th>Status</th><th /></tr></thead><tbody>{candidates.map(candidate => <tr key={candidate.id}><td>{candidate.question}</td><td>{candidate.answer}</td><td>{String(candidate.status)}</td><td><fetcher.Form method="post" className="d-inline"><input type="hidden" name="id" value={candidate.id} /><input type="hidden" name="status" value="1" /><Button variant="link" type="submit">Approve</Button></fetcher.Form>{' '}<fetcher.Form method="post" className="d-inline"><input type="hidden" name="id" value={candidate.id} /><input type="hidden" name="status" value="2" /><Button variant="link" type="submit">Reject</Button></fetcher.Form></td></tr>)}</tbody></Table></>;
}
export default Component;
