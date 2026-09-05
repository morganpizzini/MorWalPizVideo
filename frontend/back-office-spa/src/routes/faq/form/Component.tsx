import React, { useEffect, useState } from 'react';
import { Button, Form } from 'react-bootstrap';
import { useFetcher, useLoaderData, useNavigate, useParams } from 'react-router';
import type { FaqFormData } from './loader';
import PageHeader from '@components/PageHeader';

const statuses = [['0', 'Draft'], ['1', 'Published'], ['2', 'Archived']];

export default function FaqForm(): React.ReactElement {
  const { faq, categories } = useLoaderData() as FaqFormData;
  const { id } = useParams();
  const navigate = useNavigate();
  const fetcher = useFetcher();
  const [question, setQuestion] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [status, setStatus] = useState('0');
  const errors = fetcher.data?.errors as Record<string, string> | undefined;

  useEffect(() => {
    setQuestion(faq?.question ?? '');
    setCategoryId(faq?.categoryId ?? categories.find(category => category.isActive)?.id ?? '');
    setStatus(String(faq?.status ?? 0));
  }, [faq, categories]);
  useEffect(() => {
    if (fetcher.state === 'idle' && fetcher.data?.success) navigate('/faq');
  }, [fetcher.data, fetcher.state, navigate]);

  return <>
    <PageHeader title={id ? 'Edit FAQ' : 'Create FAQ'} />
    {errors?.generics && <div className="alert alert-danger">{errors.generics}</div>}
    <fetcher.Form method="post">
      <Form.Group className="mb-3"><Form.Label>Question</Form.Label><Form.Control value={question} name="question" onChange={event => setQuestion(event.target.value)} required />{errors?.question && <Form.Text className="text-danger">{errors.question}</Form.Text>}</Form.Group>
      <Form.Group className="mb-3"><Form.Label>Category</Form.Label><Form.Select name="categoryId" value={categoryId} onChange={event => setCategoryId(event.target.value)}>{categories.map(category => <option key={category.id} value={category.id}>{category.name}{category.isActive ? '' : ' (inactive)'}</option>)}</Form.Select>{errors?.categoryId && <Form.Text className="text-danger">{errors.categoryId}</Form.Text>}</Form.Group>
      <Form.Group className="mb-3"><Form.Label>Status</Form.Label><Form.Select name="status" value={status} onChange={event => setStatus(event.target.value)}>{statuses.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</Form.Select></Form.Group>
      <Button type="submit" disabled={fetcher.state !== 'idle'}>{fetcher.state === 'idle' ? 'Save FAQ' : 'Saving...'}</Button>{' '}<Button variant="secondary" onClick={() => navigate('/faq')}>Cancel</Button>
    </fetcher.Form>
  </>;
}
