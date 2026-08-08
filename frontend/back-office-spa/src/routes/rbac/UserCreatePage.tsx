import { useState, type FormEvent } from 'react';
import { Alert, Button, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { endpoints, post } from '@morwalpizvideo/services';

export default function RbacUserCreatePage() {
  const navigate = useNavigate(); const [error, setError] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); const values = Object.fromEntries(new FormData(event.currentTarget)); try { await post(endpoints.USERS, { username: values.username, email: values.email, password: values.password, isActive: values.isActive === 'on' }); navigate('/rbac/users'); } catch { setError(true); } }
  return <div className="rbac-form-width"><h1 className="h3">Create user</h1>{error ? <Alert variant="danger">Unable to create user.</Alert> : null}<Form onSubmit={submit} className="d-grid gap-3"><Form.Group><Form.Label htmlFor="username">Username</Form.Label><Form.Control id="username" name="username" required /></Form.Group><Form.Group><Form.Label htmlFor="email">Email</Form.Label><Form.Control id="email" name="email" type="email" required /></Form.Group><Form.Group><Form.Label htmlFor="password">Password</Form.Label><Form.Control id="password" name="password" type="password" required /></Form.Group><Form.Check id="isActive" name="isActive" label="Active" defaultChecked /><div><Button type="submit">Create user</Button></div></Form></div>;
}