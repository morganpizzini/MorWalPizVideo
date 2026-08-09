import { Link } from 'react-router';
import { Lightbulb, MessageCircle } from 'lucide-react';
import { hasPermission, permissions } from '../../../authorization/permissions';
import { authService } from '../../../services/authService';

export default function InsightsLandingPage() {
  const effectivePermissions = authService.getPermissions();
  const canManageTopics = hasPermission(effectivePermissions, [permissions.insights.view, permissions.insights.manage]);
  const canScanComments = hasPermission(effectivePermissions, [permissions.insights.scan, permissions.insights.manage]);

  return (
    <div>
      <h1 className="h3 mb-4">Insights</h1>
      <div className="row g-3">
        {canManageTopics ? <div className="col-md-6"><Link className="rbac-workflow-link" to="/insights/topics"><Lightbulb size={22} aria-hidden="true" /><span><strong>Topics</strong><small>Manage topics and persisted insights</small></span></Link></div> : null}
        {canScanComments ? <div className="col-md-6"><Link className="rbac-workflow-link" to="/insights/comments"><MessageCircle size={22} aria-hidden="true" /><span><strong>YouTube comments</strong><small>Analyze comments for a selected topic</small></span></Link></div> : null}
      </div>
    </div>
  );
}
