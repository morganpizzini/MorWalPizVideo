import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Form } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router';
import { ComposeUrl, endpoints, get, put } from '@morwalpizvideo/services';
import GroupForm, { toGroupDraft, type GroupDraft } from './GroupForm';
import { parsePermissions, type RbacGroup } from './types';

export default function RbacGroupEditPage() {
  const { id = '' } = useParams(); const navigate = useNavigate(); const [draft, setDraft] = useState<GroupDraft | null>(null); const [error, setError] = useState(false); const endpoint = ComposeUrl(endpoints.RBAC_GROUPS_DETAIL, { id: encodeURIComponent(id) });
  useEffect(() => { void get(endpoint).then(value => setDraft(toGroupDraft(value as RbacGroup))).catch(() => setError(true)); }, [endpoint]);
  async function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!draft) return; try { await put(endpoint, { ...draft, code: draft.code.trim().toLowerCase(), name: draft.name.trim(), description: draft.description.trim(), permissions: parsePermissions(draft.permissions) }); navigate(`/rbac/groups/${id}`); } catch { setError(true); } }
  if (!draft && !error) return <p>Loading group...</p>;
  return <div className="rbac-form-width"><h1 className="h3">Edit group</h1>{error ? <Alert variant="danger">Unable to update group.</Alert> : null}{draft ? <Form onSubmit={submit}><GroupForm draft={draft} onChange={setDraft} submitLabel="Save group" /></Form> : null}</div>;
}