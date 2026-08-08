import { useState, type FormEvent } from 'react';
import { Alert, Form } from 'react-bootstrap';
import { useNavigate } from 'react-router';
import { endpoints, post } from '@morwalpizvideo/services';
import GroupForm, { type GroupDraft } from './GroupForm';
import { parsePermissions } from './types';

export default function RbacGroupCreatePage() {
  const navigate = useNavigate(); const [draft, setDraft] = useState<GroupDraft>({ code: '', name: '', description: '', isActive: true, permissions: '' }); const [error, setError] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); try { await post(endpoints.RBAC_GROUPS, { ...draft, code: draft.code.trim().toLowerCase(), name: draft.name.trim(), description: draft.description.trim(), permissions: parsePermissions(draft.permissions) }); navigate('/rbac/groups'); } catch { setError(true); } }
  return <div className="rbac-form-width"><h1 className="h3">Create group</h1>{error ? <Alert variant="danger">Unable to create group.</Alert> : null}<Form onSubmit={submit}><GroupForm draft={draft} onChange={setDraft} submitLabel="Create group" /></Form></div>;
}