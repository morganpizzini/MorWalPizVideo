import { useEffect, useState } from 'react';
import { Alert, Table } from 'react-bootstrap';
import { Link } from 'react-router';
import { endpoints, get } from '@morwalpizvideo/services';
import type { RbacGroup } from './types';

export default function RbacGroupsPage() {
  const [groups, setGroups] = useState<RbacGroup[]>([]);
  const [error, setError] = useState(false);

  useEffect(() => {
    void get(endpoints.RBAC_GROUPS)
      .then(value => setGroups((value as RbacGroup[]) ?? []))
      .catch(() => setError(true));
  }, []);

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center gap-3 mb-3">
        <h1 className="h3 mb-0">Groups</h1>
        <Link className="btn btn-primary" to="/rbac/groups/create">Create group</Link>
      </div>
      {error ? <Alert variant="danger">Unable to load groups.</Alert> : null}
      <Table responsive hover className="align-middle">
        <thead><tr><th>Code</th><th>Name</th><th>Status</th><th>Members</th><th /></tr></thead>
        <tbody>{groups.map(group => (
          <tr key={group.id}>
            <td>{group.code}</td><td>{group.name}</td><td>{group.isActive ? 'Active' : 'Inactive'}</td><td>{group.memberCount}</td>
            <td className="text-end"><Link className="btn btn-sm btn-outline-primary" to={`/rbac/groups/${group.id}`}>View</Link></td>
          </tr>
        ))}</tbody>
      </Table>
    </div>
  );
}
