import { useEffect, useMemo, useState } from 'react';
import { Alert, Badge, Button, Card, Col, Form, Row, Table } from 'react-bootstrap';
import { ComposeUrl, Delete, endpoints, get, post, put } from '@morwalpizvideo/services';

interface RbacUserSummary {
  id: string;
  username: string;
  email: string;
  isActive: boolean;
  lastLogin?: string | null;
  groupIds: string[];
  groupCodes: string[];
  directPermissions: string[];
  effectivePermissions: string[];
  canAccessBackoffice: boolean;
}

interface RbacGroup {
  id: string;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  permissions: string[];
  memberCount: number;
}

interface GroupFormState {
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  permissionsCsv: string;
}

interface UserFormState {
  username: string;
  email: string;
  isActive: boolean;
}

interface CreateUserFormState extends UserFormState {
  password: string;
}

function parsePermissionsCsv(input: string): string[] {
  return input
    .split(',')
    .map((permission) => permission.trim().toLowerCase())
    .filter((permission) => permission.length > 0)
    .filter((permission, index, source) => source.indexOf(permission) === index);
}

function toCsv(values: string[]): string {
  return values.join(', ');
}

export default function RbacManagementPage() {
  const [users, setUsers] = useState<RbacUserSummary[]>([]);
  const [groups, setGroups] = useState<RbacGroup[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [userEditDraft, setUserEditDraft] = useState<Record<string, UserFormState>>({});
  const [passwordDraft, setPasswordDraft] = useState<Record<string, string>>({});
  const [directPermissionsDraft, setDirectPermissionsDraft] = useState<Record<string, string>>({});
  const [groupMembershipDraft, setGroupMembershipDraft] = useState<Record<string, string[]>>({});
  const [groupEditDraft, setGroupEditDraft] = useState<Record<string, GroupFormState>>({});
  const [newUser, setNewUser] = useState<CreateUserFormState>({
    username: '',
    email: '',
    password: '',
    isActive: true,
  });
  const [newGroup, setNewGroup] = useState<GroupFormState>({
    code: '',
    name: '',
    description: '',
    isActive: true,
    permissionsCsv: '',
  });

  const orderedGroups = useMemo(
    () => [...groups].sort((a, b) => a.code.localeCompare(b.code)),
    [groups]
  );

  const orderedUsers = useMemo(
    () => [...users].sort((a, b) => a.username.localeCompare(b.username)),
    [users]
  );

  async function loadData() {
    setBusy(true);
    setError(null);

    try {
      const [usersResponse, groupsResponse] = await Promise.all([
        get(endpoints.RBAC_USERS),
        get(endpoints.RBAC_GROUPS),
      ]);

      const loadedUsers = (usersResponse as RbacUserSummary[]) ?? [];
      const loadedGroups = (groupsResponse as RbacGroup[]) ?? [];

      setUsers(loadedUsers);
      setGroups(loadedGroups);
      setUserEditDraft(
        Object.fromEntries(
          loadedUsers.map((user) => [
            user.id,
            {
              username: user.username,
              email: user.email,
              isActive: user.isActive,
            },
          ])
        )
      );
      setPasswordDraft(Object.fromEntries(loadedUsers.map((user) => [user.id, ''])));
      setDirectPermissionsDraft(
        Object.fromEntries(
          loadedUsers.map((user) => [user.id, toCsv(user.directPermissions)])
        )
      );
      setGroupMembershipDraft(
        Object.fromEntries(
          loadedUsers.map((user) => [user.id, [...user.groupIds]])
        )
      );
      setGroupEditDraft(
        Object.fromEntries(
          loadedGroups.map((group) => [
            group.id,
            {
              code: group.code,
              name: group.name,
              description: group.description,
              isActive: group.isActive,
              permissionsCsv: toCsv(group.permissions),
            },
          ])
        )
      );
    } catch {
      setError('Impossibile caricare dati RBAC.');
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void loadData();
  }, []);

  async function createUser() {
    if (!newUser.username.trim() || !newUser.email.trim() || !newUser.password.trim()) {
      setError('Username, email e password sono obbligatori.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await post(endpoints.USERS, {
        username: newUser.username.trim(),
        email: newUser.email.trim(),
        password: newUser.password,
        isActive: newUser.isActive,
      });

      setNewUser({
        username: '',
        email: '',
        password: '',
        isActive: true,
      });
      setSuccess('Utente creato.');
      await loadData();
    } catch {
      setError('Creazione utente non riuscita.');
      setBusy(false);
    }
  }

  async function saveUser(userId: string) {
    const draft = userEditDraft[userId];
    if (!draft || !draft.username.trim() || !draft.email.trim()) {
      setError('Username e email utente sono obbligatori.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await put(ComposeUrl(endpoints.USER_DETAIL, { id: encodeURIComponent(userId) }), {
        username: draft.username.trim(),
        email: draft.email.trim(),
        isActive: draft.isActive,
      });

      setSuccess('Utente aggiornato.');
      await loadData();
    } catch {
      setError('Aggiornamento utente non riuscito.');
      setBusy(false);
    }
  }

  async function saveUserStatus(userId: string) {
    const draft = userEditDraft[userId];
    if (!draft) {
      setError('Stato utente non disponibile.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await put(ComposeUrl(endpoints.USER_STATUS, { id: encodeURIComponent(userId) }), {
        isActive: draft.isActive,
      });

      setSuccess('Stato utente aggiornato.');
      await loadData();
    } catch {
      setError('Aggiornamento stato utente non riuscito.');
      setBusy(false);
    }
  }

  async function saveUserPassword(userId: string, mode: 'reset' | 'set') {
    const nextPassword = passwordDraft[userId] ?? '';
    if (!nextPassword.trim()) {
      setError('La nuova password amministrativa e obbligatoria.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      const endpoint = mode === 'reset' ? endpoints.USER_PASSWORD_RESET : endpoints.USER_PASSWORD_SET;
      await put(ComposeUrl(endpoint, { id: encodeURIComponent(userId) }), {
        newPassword: nextPassword,
      });

      setPasswordDraft((current) => ({
        ...current,
        [userId]: '',
      }));
      setSuccess(mode === 'reset' ? 'Password utente resettata.' : 'Password utente impostata.');
    } catch {
      setError(mode === 'reset' ? 'Reset password utente non riuscito.' : 'Impostazione password utente non riuscita.');
    } finally {
      setBusy(false);
    }
  }

  async function saveUserPermissions(userId: string) {
    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await put(ComposeUrl(endpoints.RBAC_USER_PERMISSIONS, { id: encodeURIComponent(userId) }), {
        permissions: parsePermissionsCsv(directPermissionsDraft[userId] ?? ''),
      });

      setSuccess('Permessi utente aggiornati.');
      await loadData();
    } catch {
      setError('Aggiornamento permessi utente non riuscito.');
      setBusy(false);
    }
  }

  async function saveUserGroups(userId: string) {
    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await put(ComposeUrl(endpoints.RBAC_USER_GROUPS, { id: encodeURIComponent(userId) }), {
        groupIds: groupMembershipDraft[userId] ?? [],
      });

      setSuccess('Gruppi utente aggiornati.');
      await loadData();
    } catch {
      setError('Aggiornamento gruppi utente non riuscito.');
      setBusy(false);
    }
  }

  async function createGroup() {
    if (!newGroup.code.trim() || !newGroup.name.trim()) {
      setError('Code e Name del gruppo sono obbligatori.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await post(endpoints.RBAC_GROUPS, {
        code: newGroup.code,
        name: newGroup.name,
        description: newGroup.description,
        isActive: newGroup.isActive,
        permissions: parsePermissionsCsv(newGroup.permissionsCsv),
      });

      setNewGroup({
        code: '',
        name: '',
        description: '',
        isActive: true,
        permissionsCsv: '',
      });
      setSuccess('Gruppo creato.');
      await loadData();
    } catch {
      setError('Creazione gruppo non riuscita.');
      setBusy(false);
    }
  }

  async function saveGroup(groupId: string) {
    const draft = groupEditDraft[groupId];
    if (!draft || !draft.code.trim() || !draft.name.trim()) {
      setError('Code e Name del gruppo sono obbligatori.');
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await put(ComposeUrl(endpoints.RBAC_GROUPS_DETAIL, { id: encodeURIComponent(groupId) }), {
        code: draft.code,
        name: draft.name,
        description: draft.description,
        isActive: draft.isActive,
        permissions: parsePermissionsCsv(draft.permissionsCsv),
      });

      setSuccess('Gruppo aggiornato.');
      await loadData();
    } catch {
      setError('Aggiornamento gruppo non riuscito.');
      setBusy(false);
    }
  }

  async function deleteGroup(groupId: string) {
    if (!window.confirm('Eliminare questo gruppo? I membri perderanno la membership.')) {
      return;
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await Delete(ComposeUrl(endpoints.RBAC_GROUPS_DETAIL, { id: encodeURIComponent(groupId) }));
      setSuccess('Gruppo eliminato.');
      await loadData();
    } catch {
      setError('Eliminazione gruppo non riuscita.');
      setBusy(false);
    }
  }

  return (
    <div>
      <h1>RBAC Management</h1>
      <p className="text-muted">
        Gestione utenti, gruppi MongoDB, permessi diretti e permessi ereditati.
      </p>

      {error ? <Alert variant="danger">{error}</Alert> : null}
      {success ? <Alert variant="success">{success}</Alert> : null}

      <Card className="mb-4">
        <Card.Header>Crea utente</Card.Header>
        <Card.Body>
          <Row className="g-3">
            <Col md={3}>
              <Form.Group controlId="create-user-username">
                <Form.Label>Username</Form.Label>
                <Form.Control
                  value={newUser.username}
                  onChange={(event) =>
                    setNewUser((current) => ({ ...current, username: event.target.value }))
                  }
                  placeholder="editor01"
                />
              </Form.Group>
            </Col>
            <Col md={3}>
              <Form.Group controlId="create-user-email">
                <Form.Label>Email</Form.Label>
                <Form.Control
                  type="email"
                  value={newUser.email}
                  onChange={(event) =>
                    setNewUser((current) => ({ ...current, email: event.target.value }))
                  }
                  placeholder="editor@example.test"
                />
              </Form.Group>
            </Col>
            <Col md={2}>
              <Form.Group controlId="create-user-password">
                <Form.Label>Password</Form.Label>
                <Form.Control
                  type="password"
                  value={newUser.password}
                  onChange={(event) =>
                    setNewUser((current) => ({ ...current, password: event.target.value }))
                  }
                />
              </Form.Group>
            </Col>
            <Col md={2} className="d-flex align-items-end">
              <Form.Check
                type="switch"
                id="create-user-active"
                label="Active"
                checked={newUser.isActive}
                onChange={(event) =>
                  setNewUser((current) => ({ ...current, isActive: event.target.checked }))
                }
              />
            </Col>
            <Col md={12} className="d-flex justify-content-end">
              <Button disabled={busy} onClick={createUser}>
                Create user
              </Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>

      <Card className="mb-4">
        <Card.Header>Crea gruppo</Card.Header>
        <Card.Body>
          <Row className="g-3">
            <Col md={3}>
              <Form.Label>Code</Form.Label>
              <Form.Control
                value={newGroup.code}
                onChange={(event) => setNewGroup((current) => ({ ...current, code: event.target.value }))}
                placeholder="admin"
              />
            </Col>
            <Col md={3}>
              <Form.Label>Name</Form.Label>
              <Form.Control
                value={newGroup.name}
                onChange={(event) => setNewGroup((current) => ({ ...current, name: event.target.value }))}
                placeholder="Administrators"
              />
            </Col>
            <Col md={4}>
              <Form.Label>Description</Form.Label>
              <Form.Control
                value={newGroup.description}
                onChange={(event) =>
                  setNewGroup((current) => ({ ...current, description: event.target.value }))
                }
              />
            </Col>
            <Col md={2} className="d-flex align-items-end">
              <Form.Check
                type="switch"
                id="create-group-active"
                label="Active"
                checked={newGroup.isActive}
                onChange={(event) =>
                  setNewGroup((current) => ({ ...current, isActive: event.target.checked }))
                }
              />
            </Col>
            <Col md={10}>
              <Form.Label>Permissions (comma separated)</Form.Label>
              <Form.Control
                value={newGroup.permissionsCsv}
                onChange={(event) =>
                  setNewGroup((current) => ({ ...current, permissionsCsv: event.target.value }))
                }
                placeholder="canaccessbackoffice, diagnostics.read"
              />
            </Col>
            <Col md={2} className="d-flex align-items-end">
              <Button disabled={busy} onClick={createGroup} className="w-100">
                Create
              </Button>
            </Col>
          </Row>
        </Card.Body>
      </Card>

      <Card className="mb-4">
        <Card.Header>Gruppi</Card.Header>
        <Card.Body className="p-0">
          <Table striped responsive className="mb-0 align-middle">
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Description</th>
                <th>Active</th>
                <th>Permissions</th>
                <th>Members</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {orderedGroups.map((group) => {
                const draft = groupEditDraft[group.id];
                if (!draft) {
                  return null;
                }

                return (
                  <tr key={group.id}>
                    <td>
                      <Form.Control
                        size="sm"
                        value={draft.code}
                        onChange={(event) =>
                          setGroupEditDraft((current) => ({
                            ...current,
                            [group.id]: { ...draft, code: event.target.value },
                          }))
                        }
                      />
                    </td>
                    <td>
                      <Form.Control
                        size="sm"
                        value={draft.name}
                        onChange={(event) =>
                          setGroupEditDraft((current) => ({
                            ...current,
                            [group.id]: { ...draft, name: event.target.value },
                          }))
                        }
                      />
                    </td>
                    <td>
                      <Form.Control
                        size="sm"
                        value={draft.description}
                        onChange={(event) =>
                          setGroupEditDraft((current) => ({
                            ...current,
                            [group.id]: { ...draft, description: event.target.value },
                          }))
                        }
                      />
                    </td>
                    <td>
                      <Form.Check
                        type="switch"
                        checked={draft.isActive}
                        onChange={(event) =>
                          setGroupEditDraft((current) => ({
                            ...current,
                            [group.id]: { ...draft, isActive: event.target.checked },
                          }))
                        }
                      />
                    </td>
                    <td>
                      <Form.Control
                        size="sm"
                        value={draft.permissionsCsv}
                        onChange={(event) =>
                          setGroupEditDraft((current) => ({
                            ...current,
                            [group.id]: { ...draft, permissionsCsv: event.target.value },
                          }))
                        }
                        placeholder="perm1, perm2"
                      />
                    </td>
                    <td>{group.memberCount}</td>
                    <td className="d-flex gap-2">
                      <Button size="sm" disabled={busy} onClick={() => saveGroup(group.id)}>
                        Save
                      </Button>
                      <Button
                        size="sm"
                        variant="outline-danger"
                        disabled={busy}
                        onClick={() => deleteGroup(group.id)}
                      >
                        Delete
                      </Button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </Card.Body>
      </Card>

      <Card>
        <Card.Header>Utenti</Card.Header>
        <Card.Body className="p-0">
          <Table striped responsive className="mb-0 align-middle">
            <thead>
              <tr>
                <th>Account</th>
                <th>Active</th>
                <th>Groups</th>
                <th>Direct permissions</th>
                <th>Effective permissions</th>
                <th>Password</th>
                <th>BackOffice</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {orderedUsers.map((user) => {
                const userDraft = userEditDraft[user.id];
                if (!userDraft) {
                  return null;
                }

                return (
                  <tr key={user.id}>
                    <td>
                      <Form.Group className="mb-2">
                        <Form.Label visuallyHidden>Username {user.username}</Form.Label>
                        <Form.Control
                          size="sm"
                          aria-label={`Username for ${user.username}`}
                          value={userDraft.username}
                          onChange={(event) =>
                            setUserEditDraft((current) => ({
                              ...current,
                              [user.id]: { ...userDraft, username: event.target.value },
                            }))
                          }
                        />
                      </Form.Group>
                      <Form.Group>
                        <Form.Label visuallyHidden>Email {user.username}</Form.Label>
                        <Form.Control
                          size="sm"
                          type="email"
                          aria-label={`Email for ${user.username}`}
                          value={userDraft.email}
                          onChange={(event) =>
                            setUserEditDraft((current) => ({
                              ...current,
                              [user.id]: { ...userDraft, email: event.target.value },
                            }))
                          }
                        />
                      </Form.Group>
                      <div className="text-muted small mt-2">
                        Last login: {user.lastLogin ? new Date(user.lastLogin).toLocaleString() : 'never'}
                      </div>
                    </td>
                    <td>
                      <Form.Check
                        type="switch"
                        id={`user-active-${user.id}`}
                        label={userDraft.isActive ? 'Active' : 'Inactive'}
                        aria-label={`Active status for ${user.username}`}
                        checked={userDraft.isActive}
                        onChange={(event) =>
                          setUserEditDraft((current) => ({
                            ...current,
                            [user.id]: { ...userDraft, isActive: event.target.checked },
                          }))
                        }
                      />
                    </td>
                    <td>
                      <Form.Select
                        size="sm"
                        multiple
                        aria-label={`Groups for ${user.username}`}
                        value={groupMembershipDraft[user.id] ?? []}
                        onChange={(event) => {
                          const selected = Array.from(event.target.selectedOptions).map((option) => option.value);
                          setGroupMembershipDraft((current) => ({
                            ...current,
                            [user.id]: selected,
                          }));
                        }}
                        style={{ minWidth: 220, minHeight: 110 }}
                      >
                        {orderedGroups.map((group) => (
                          <option key={group.id} value={group.id}>
                            {group.code} ({group.name})
                          </option>
                        ))}
                      </Form.Select>
                      <div className="mt-2">
                        {user.groupCodes.map((groupCode) => (
                          <Badge key={`${user.id}-${groupCode}`} bg="secondary" className="me-1">
                            {groupCode}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td>
                      <Form.Control
                        size="sm"
                        aria-label={`Direct permissions for ${user.username}`}
                        value={directPermissionsDraft[user.id] ?? ''}
                        onChange={(event) =>
                          setDirectPermissionsDraft((current) => ({
                            ...current,
                            [user.id]: event.target.value,
                          }))
                        }
                        placeholder="perm1, perm2"
                      />
                    </td>
                    <td>
                      <div style={{ maxWidth: 320 }}>
                        {user.effectivePermissions.map((permission) => (
                          <Badge key={`${user.id}-${permission}`} bg="info" className="me-1 mb-1">
                            {permission}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td>
                      <Form.Control
                        size="sm"
                        type="password"
                        aria-label={`New password for ${user.username}`}
                        value={passwordDraft[user.id] ?? ''}
                        onChange={(event) =>
                          setPasswordDraft((current) => ({
                            ...current,
                            [user.id]: event.target.value,
                          }))
                        }
                        placeholder="Nuova password"
                        className="mb-2"
                      />
                      <div className="d-flex gap-2 flex-wrap">
                        <Button
                          size="sm"
                          variant="outline-warning"
                          disabled={busy}
                          onClick={() => saveUserPassword(user.id, 'reset')}
                        >
                          Reset password
                        </Button>
                        <Button
                          size="sm"
                          variant="outline-secondary"
                          disabled={busy}
                          onClick={() => saveUserPassword(user.id, 'set')}
                        >
                          Set password
                        </Button>
                      </div>
                    </td>
                    <td>{user.canAccessBackoffice ? 'yes' : 'no'}</td>
                    <td className="d-flex gap-2 flex-wrap">
                      <Button size="sm" disabled={busy} onClick={() => saveUser(user.id)}>
                        Save user
                      </Button>
                      <Button size="sm" variant="outline-primary" disabled={busy} onClick={() => saveUserStatus(user.id)}>
                        Save status
                      </Button>
                      <Button size="sm" disabled={busy} onClick={() => saveUserGroups(user.id)}>
                        Save groups
                      </Button>
                      <Button size="sm" disabled={busy} onClick={() => saveUserPermissions(user.id)}>
                        Save permissions
                      </Button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </Card.Body>
      </Card>
    </div>
  );
}
