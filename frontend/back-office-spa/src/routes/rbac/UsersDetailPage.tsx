import { useEffect, useState } from 'react';
import { Alert, Badge, Button, Form } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router';
import { ComposeUrl, Delete, endpoints, get, put } from '@morwalpizvideo/services';
import { hasPermission, permissions } from '../../authorization/permissions';
import { authService } from '../../services/authService';
import { parsePermissions, type RbacGroup, type RbacUserSummary } from './types';

export default function RbacUserDetailPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const [user, setUser] = useState<RbacUserSummary | null>(null);
  const [groups, setGroups] = useState<RbacGroup[]>([]);
  const [groupIds, setGroupIds] = useState<string[]>([]);
  const [directPermissions, setDirectPermissions] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState('');
  const effectivePermissions = authService.getPermissions();
  const canUpdateUsers = hasPermission(effectivePermissions, [permissions.users.update]);
  const canDeleteUsers = hasPermission(effectivePermissions, [permissions.users.delete]);
  const canManagePermissions = hasPermission(effectivePermissions, [permissions.users.permissionsManage]);

  useEffect(() => {
    const loadUser = async () => {
      const loadedUser = await get(ComposeUrl(endpoints.RBAC_USER_DETAIL, { id: encodeURIComponent(id) })) as RbacUserSummary;
      setUser(loadedUser);
      setGroupIds(loadedUser.groupIds);
      setDirectPermissions(loadedUser.directPermissions.join(', '));

      if (canManagePermissions) {
        const loadedGroups = await get(endpoints.RBAC_GROUPS) as RbacGroup[];
        setGroups(loadedGroups ?? []);
      }
    };

    void loadUser();
  }, [canManagePermissions, id]);

  if (!user) return <p>Loading user...</p>;
  const endpoint = (value: string) => ComposeUrl(value, { id: encodeURIComponent(id) });

  async function saveProfile() {
    await put(endpoint(endpoints.USER_DETAIL), { username: user!.username, email: user!.email, isActive: user!.isActive });
    setMessage('User updated.');
  }

  async function savePassword(action: 'reset' | 'set') {
    const passwordEndpoint = action === 'reset' ? endpoints.USER_PASSWORD_RESET : endpoints.USER_PASSWORD_SET;
    await put(endpoint(passwordEndpoint), { newPassword: password });
    setPassword('');
    setMessage('Password updated.');
  }

  return (
    <div className="rbac-form-width">
      <h1 className="h3">{user.username}</h1>
      {message ? <Alert variant="success">{message}</Alert> : null}
      <dl className="row">
        <dt className="col-sm-4">Email</dt><dd className="col-sm-8">{user.email}</dd>
        <dt className="col-sm-4">Effective permissions</dt>
        <dd className="col-sm-8">{user.effectivePermissions.map(value => <Badge key={value} bg="secondary" className="me-1">{value}</Badge>)}</dd>
      </dl>

      {canUpdateUsers ? <section><hr /><h2 className="h5">Account</h2>
        <Form.Group className="mb-2"><Form.Label htmlFor="user-username">Username</Form.Label><Form.Control id="user-username" value={user.username} onChange={event => setUser({ ...user, username: event.target.value })} /></Form.Group>
        <Form.Group className="mb-2"><Form.Label htmlFor="user-email">Email</Form.Label><Form.Control id="user-email" type="email" value={user.email} onChange={event => setUser({ ...user, email: event.target.value })} /></Form.Group>
        <Button variant="outline-primary" onClick={() => void saveProfile()}>Save user</Button>
        <hr /><h2 className="h5">Status</h2><Form.Check id="user-active" label="Active" checked={user.isActive} onChange={event => setUser({ ...user, isActive: event.target.checked })} />
        <Button className="mt-2" variant="outline-primary" onClick={() => void put(endpoint(endpoints.USER_STATUS), { isActive: user.isActive }).then(() => setMessage('Status updated.'))}>Save status</Button>
      </section> : null}

      {canManagePermissions ? <section><hr /><h2 className="h5">Group membership</h2>
        <Form.Select multiple value={groupIds} onChange={event => setGroupIds(Array.from(event.target.selectedOptions, option => option.value))} aria-label="Groups">{groups.map(group => <option key={group.id} value={group.id}>{group.code} - {group.name}</option>)}</Form.Select>
        <Button className="mt-2" variant="outline-primary" onClick={() => void put(endpoint(endpoints.RBAC_USER_GROUPS), { groupIds }).then(() => setMessage('Groups updated.'))}>Save groups</Button>
        <hr /><h2 className="h5">Direct permissions</h2>
        <Form.Control as="textarea" rows={3} value={directPermissions} onChange={event => setDirectPermissions(event.target.value)} aria-label="Direct permissions" />
        <Button className="mt-2" variant="outline-primary" onClick={() => void put(endpoint(endpoints.RBAC_USER_PERMISSIONS), { permissions: parsePermissions(directPermissions) }).then(() => setMessage('Permissions updated.'))}>Save permissions</Button>
      </section> : null}

      {canUpdateUsers ? <section><hr /><h2 className="h5">Password</h2><Form.Control type="password" value={password} onChange={event => setPassword(event.target.value)} aria-label="New password" /><div className="d-flex gap-2 mt-2"><Button variant="outline-warning" onClick={() => void savePassword('reset')}>Reset password</Button><Button variant="outline-secondary" onClick={() => void savePassword('set')}>Set password</Button></div></section> : null}
      {canDeleteUsers ? <><hr /><Button variant="danger" onClick={() => void Delete(endpoint(endpoints.USER_DETAIL)).then(() => navigate('/rbac/users'))}>Delete user</Button></> : null}
    </div>
  );
}
