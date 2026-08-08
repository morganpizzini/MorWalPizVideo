import { Link } from 'react-router';
import { ShieldCheck, Users } from 'lucide-react';
import { hasPermission, permissions } from '../../authorization/permissions';
import { authService } from '../../services/authService';

export default function RbacManagementPage() {
  const effectivePermissions = authService.getPermissions();
  const canViewUsers = hasPermission(effectivePermissions, [
    permissions.users.view,
    permissions.users.manage,
    permissions.users.permissionsManage,
  ]);
  const canManagePermissions = hasPermission(effectivePermissions, [permissions.users.permissionsManage]);

  return (
    <div>
      <h1 className="h3 mb-4">Users &amp; access</h1>
      <div className="row g-3">
        {canViewUsers ? (
          <div className="col-md-6">
            <Link className="rbac-workflow-link" to="/rbac/users">
              <Users size={22} aria-hidden="true" />
              <span><strong>Users</strong><small>Accounts and lifecycle status</small></span>
            </Link>
          </div>
        ) : null}
        {canManagePermissions ? (
          <div className="col-md-6">
            <Link className="rbac-workflow-link" to="/rbac/groups">
              <ShieldCheck size={22} aria-hidden="true" />
              <span><strong>Groups</strong><small>Reusable permission sets and membership</small></span>
            </Link>
          </div>
        ) : null}
      </div>
    </div>
  );
}
