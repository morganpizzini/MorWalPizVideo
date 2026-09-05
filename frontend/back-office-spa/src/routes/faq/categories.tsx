import React, { useEffect, useState } from 'react';
import { Button, Form, Table } from 'react-bootstrap';
import { useFetcher, useLoaderData, Link } from 'react-router';
import { fetchFaqCategories, saveFaqCategory } from '@morwalpizvideo/services';
import type { FaqCategoryAdmin } from '@morwalpizvideo/models';
import PageHeader from '@components/PageHeader';

export async function Loader(): Promise<FaqCategoryAdmin[]> { return fetchFaqCategories(); }
export async function Action({ request }: { request: Request }) {
  const values = Object.fromEntries(await request.formData());
  try { await saveFaqCategory(String(values.id || '') || undefined, { name: String(values.name || ''), slug: String(values.slug || ''), description: String(values.description || ''), sortOrder: Number(values.sortOrder || 0), isActive: values.isActive === 'on' }); return { success: true }; }
  catch { return { errors: ['Unable to save category.'] }; }
}
export function Component(): React.ReactElement {
  const categories = useLoaderData() as FaqCategoryAdmin[];
  const fetcher = useFetcher();
  const [editing, setEditing] = useState<FaqCategoryAdmin | null>(null);
  useEffect(() => { if (fetcher.state === 'idle' && fetcher.data?.success) setEditing(null); }, [fetcher.data, fetcher.state]);
  return <><PageHeader title="FAQ categories" /><Link className="btn btn-secondary mb-3" to="/faq">Back to FAQs</Link><Table striped><thead><tr><th>Name</th><th>Slug</th><th>Active</th><th /></tr></thead><tbody>{categories.map(category => <tr key={category.id}><td>{category.name}</td><td>{category.slug}</td><td>{category.isActive ? 'Yes' : 'No'}</td><td><Button variant="link" onClick={() => setEditing(category)}>Edit</Button></td></tr>)}</tbody></Table><h2 className="h4">{editing ? 'Edit category' : 'New category'}</h2><fetcher.Form method="post"><input type="hidden" name="id" value={editing?.id ?? ''} /><Form.Control className="mb-2" name="name" placeholder="Name" defaultValue={editing?.name ?? ''} required /><Form.Control className="mb-2" name="slug" placeholder="Slug" defaultValue={editing?.slug ?? ''} /><Form.Control className="mb-2" name="description" placeholder="Description" defaultValue={editing?.description ?? ''} /><Form.Control className="mb-2" name="sortOrder" type="number" placeholder="Sort order" defaultValue={editing?.sortOrder ?? 0} /><Form.Check className="mb-2" name="isActive" label="Active" defaultChecked={editing?.isActive ?? true} /><Button type="submit">Save category</Button></fetcher.Form></>;
}
export default Component;
