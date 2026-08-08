import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router';
import { ComposeUrl, endpoints, get, put } from '@morwalpizvideo/services';
import type { RbacUserSummary } from './types';

export default function RbacUserEditPage() {
  const { id = '' } = useParams(); const navigate = useNavigate();
  const [user, setUser] = useState<RbacUserSummary | null>(null); const [error, setError] = useState(false);
  useEffect(() => { void get(ComposeUrl(endpoints.RBAC_USER_DETAIL, { id: encodeURIComponent(id) })).then(value => setUser(value as RbacUserSummary)).catch(() => setError(true)); }, [id]);
  async function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!user) return; try { await put(ComposeUrl(endpoints.USER_DETAIL, { id: encodeURIComponent(id) }), { username: user.username.trim(), email: user.email.trim(), isActive: user.isActive }); navigate(`/rbac/users/${id}`); } catch { setError(true); } }
  if (!user && !error) return <p>Loading user...</p>;
  return <div className="rbac-form-width"><h1 className="h3">Edit user</h1>{error ? <Alert variant="danger">Unable to update user.</Alert> : null}{user ? <Form onSubmit={submit} className="d-grid gap-3">
    <Form.Group><Form.Label htmlFor="username">Username</Form.Label><Form.Control id="username" value={user.username} onChange={event => setUser({ ...user, username: event.target.value })} required /></Form.Group>
    <Form.Group><Form.Label htmlFor="email">Email</Form.Label><Form.Control id="email" type="email" value={user.email} onChange={event => setUser({ ...user, email: event.target.value })} required /></Form.Group>
    <Form.Check id="isActive" label="Active" checked={user.isActive} onChange={event => setUser({ ...user, isActive: event.target.checked })} /><div><Button type="submit">Save user</Button></div>
  </Form> : null}</div>;
}