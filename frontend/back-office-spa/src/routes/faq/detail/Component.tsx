import React, { useEffect, useState } from 'react';
import { Button, Form, Table } from 'react-bootstrap';
import { Link, useFetcher, useLoaderData } from 'react-router';
import type { FaqDetailData } from './loader';
import PageHeader from '@components/PageHeader';

const statuses = ['Draft', 'Published', 'Archived'];

export default function FaqDetail(): React.ReactElement {
  const { faq, answers, categories } = useLoaderData() as FaqDetailData;
  const fetcher = useFetcher();
  const [editingId, setEditingId] = useState('');
  const [content, setContent] = useState('');
  const [status, setStatus] = useState('0');
  const category = categories.find(item => item.id === faq.categoryId);
  const errors = fetcher.data?.errors as Record<string, string> | undefined;

  useEffect(() => {
    if (fetcher.state === 'idle' && fetcher.data?.success) { setEditingId(''); setContent(''); setStatus('0'); }
  }, [fetcher.data, fetcher.state]);
  const edit = (answer: FaqDetailData['answers'][number]) => { setEditingId(answer.id); setContent(answer.content); setStatus(String(answer.status)); };

  return <>
    <PageHeader title="FAQ details" />
    <dl className="row"><dt className="col-sm-2">Question</dt><dd className="col-sm-10">{faq.question}</dd><dt className="col-sm-2">Category</dt><dd className="col-sm-10">{category?.name ?? 'Unknown'}</dd><dt className="col-sm-2">Status</dt><dd className="col-sm-10">{statuses[Number(faq.status)] ?? 'Unknown'}</dd></dl>
    <h2 className="h4">Channel answers</h2>
    <Table striped responsive><thead><tr><th>Channel</th><th>Answer</th><th>Status</th><th>Votes</th><th /></tr></thead><tbody>{answers.map(answer => <tr key={answer.id}><td>{answer.channelId}</td><td>{answer.content}</td><td>{statuses[Number(answer.status)] ?? 'Unknown'}</td><td>{answer.helpfulVotes} / {answer.notHelpfulVotes}</td><td><Button variant="link" onClick={() => edit(answer)}>Edit</Button></td></tr>)}</tbody></Table>
    <h2 className="h4">{editingId ? 'Edit channel answer' : 'Add channel answer'}</h2>
    {errors?.generics && <div className="alert alert-danger">{errors.generics}</div>}
    <fetcher.Form method="post"><input type="hidden" name="answerId" value={editingId} /><Form.Group className="mb-3"><Form.Label>Answer</Form.Label><Form.Control as="textarea" rows={4} name="content" value={content} onChange={event => setContent(event.target.value)} />{errors?.content && <Form.Text className="text-danger">{errors.content}</Form.Text>}</Form.Group><Form.Group className="mb-3"><Form.Label>Status</Form.Label><Form.Select name="status" value={status} onChange={event => setStatus(event.target.value)}>{statuses.map((label, value) => <option key={label} value={value}>{label}</option>)}</Form.Select></Form.Group><Button type="submit" disabled={fetcher.state !== 'idle'}>Save answer</Button>{' '}<Link className="btn btn-secondary" to="/faq">Back</Link></fetcher.Form>
  </>;
}
