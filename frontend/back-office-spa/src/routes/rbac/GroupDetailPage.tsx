import { useEffect, useState } from 'react';
import { Alert, Badge, Button } from 'react-bootstrap';
import { Link, useNavigate, useParams } from 'react-router';
import { ComposeUrl, Delete, endpoints, get } from '@morwalpizvideo/services';
import type { RbacGroup } from './types';

export default function RbacGroupDetailPage() {
  const { id = '' } = useParams(); const navigate = useNavigate(); const [group, setGroup] = useState<RbacGroup | null>(null); const [error, setError] = useState(false); const endpoint = ComposeUrl(endpoints.RBAC_GROUPS_DETAIL, { id: encodeURIComponent(id) });
  useEffect(() => { void get(endpoint).then(value => setGroup(value as RbacGroup)).catch(() => setError(true)); }, [endpoint]);
  async function remove() { if (!window.confirm('Delete this group? Members will lose this membership.')) return; try { await Delete(endpoint); navigate('/rbac/groups'); } catch { setError(true); } }
  if (!group && !error) return <p>Loading group...</p>;
  return <div className="rbac-form-width">{error ? <Alert variant="danger">Unable to load group.</Alert> : null}{group ? <><div className="d-flex justify-content-between align-items-center gap-3 mb-3"><h1 className="h3 mb-0">{group.name}</h1><Link className="btn btn-outline-primary" to={`/rbac/groups/${id}/edit`}>Edit</Link></div><dl className="row"><dt className="col-sm-4">Code</dt><dd className="col-sm-8">{group.code}</dd><dt className="col-sm-4">Status</dt><dd className="col-sm-8">{group.isActive ? 'Active' : 'Inactive'}</dd><dt className="col-sm-4">Members</dt><dd className="col-sm-8">{group.memberCount}</dd><dt className="col-sm-4">Description</dt><dd className="col-sm-8">{group.description || 'None'}</dd><dt className="col-sm-4">Permissions</dt><dd className="col-sm-8">{group.permissions.map(permission => <Badge key={permission} bg="secondary" className="me-1 mb-1">{permission}</Badge>)}</dd></dl><Button variant="outline-danger" onClick={() => void remove()}>Delete group</Button></> : null}</div>;
}