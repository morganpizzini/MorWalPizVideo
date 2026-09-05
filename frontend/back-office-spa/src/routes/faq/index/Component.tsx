import React, { useMemo, useState } from 'react';
import { Form, Table } from 'react-bootstrap';
import { Link, useLoaderData } from 'react-router';
import type { FaqAdmin } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';
import type { FaqIndexData } from './loader';

const statuses = ['Draft', 'Published', 'Archived'];

export default function FaqIndex(): React.ReactElement {
  const { faqs, categories } = useLoaderData() as FaqIndexData;
  const [search, setSearch] = useState('');
  const categoryNames = useMemo(() => new Map(categories.map(category => [category.id, category.name])), [categories]);
  const filtered = faqs.filter(faq => faq.question.toLowerCase().includes(search.toLowerCase()));

  return <>
    <PageHeader title="FAQs" createLink="create" />
    <div className="d-flex gap-2 mb-3">
      <Form.Control placeholder="Search questions" value={search} onChange={event => setSearch(event.target.value)} />
      <Link className="btn btn-outline-secondary" to="categories">Categories</Link>
      <Link className="btn btn-outline-secondary" to="candidates">Candidates</Link>
    </div>
    <Table striped responsive>
      <thead><tr><th>Question</th><th>Category</th><th>Status</th><th>Updated</th><th /></tr></thead>
      <tbody>{filtered.map((faq: FaqAdmin) => <tr key={faq.id}>
        <td>{faq.question}</td>
        <td>{categoryNames.get(faq.categoryId) ?? 'Unknown'}</td>
        <td>{statuses[Number(faq.status)] ?? 'Unknown'}</td>
        <td>{new Date(faq.updatedAt).toLocaleString()}</td>
        <td><Link to={faq.id}>Details</Link> <Link to={`${faq.id}/edit`}>Edit</Link></td>
      </tr>)}</tbody>
    </Table>
    {filtered.length === 0 && <p className="text-muted">No FAQs found.</p>}
  </>;
}
