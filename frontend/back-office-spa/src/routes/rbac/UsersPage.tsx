import { useEffect, useState } from 'react';
import { Alert, Table } from 'react-bootstrap';
import { Link } from 'react-router';
import { endpoints, get } from '@morwalpizvideo/services';
import { hasPermission, permissions } from '../../authorization/permissions';
import { useAppStore } from '../../state/appStore';
import type { RbacUserSummary } from './types';

export default function RbacUsersPage() {
  const [users, setUsers] = useState<RbacUserSummary[]>([]);
  const [error, setError] = useState(false);
  const effectivePermissions = useAppStore(state => state.effectivePermissions);
  const canCreateUsers = hasPermission(effectivePermissions, [permissions.users.create]);
  useEffect(() => { void get(endpoints.RBAC_USERS).then(value => setUsers((value as RbacUserSummary[]) ?? [])).catch(() => setError(true)); }, []);
  return <div><div className="d-flex justify-content-between align-items-center gap-3 mb-3"><h1 className="h3 mb-0">Users</h1>{canCreateUsers ? <Link className="btn btn-primary" to="/rbac/users/create">Create user</Link> : null}</div>{error ? <Alert variant="danger">Unable to load users.</Alert> : null}<Table responsive hover className="align-middle"><thead><tr><th>Username</th><th>Email</th><th>Status</th><th>Groups</th><th /></tr></thead><tbody>{users.map(user => <tr key={user.id}><td>{user.username}</td><td>{user.email}</td><td>{user.isActive ? 'Active' : 'Inactive'}</td><td>{user.groupCodes.join(', ') || 'None'}</td><td className="text-end"><Link className="btn btn-sm btn-outline-primary" to={`/rbac/users/${user.id}`}>Manage access</Link></td></tr>)}</tbody></Table></div>;
}