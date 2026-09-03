import React from 'react';
import { Alert, Table } from 'react-bootstrap';
import type { AuditLog } from '@/models/auditLog';

type AuditLogListProps = {
  logs: AuditLog[];
  loading?: boolean;
  error?: string;
  emptyMessage?: string;
};

const formatJson = (value?: string | null) => {
  if (!value) return null;
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
};

const AuditLogList: React.FC<AuditLogListProps> = ({ logs, loading, error, emptyMessage = 'No logs found.' }) => {
  if (loading) return <p className="text-muted">Loading logs...</p>;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (logs.length === 0) return <p className="text-muted">{emptyMessage}</p>;

  return (
    <Table responsive hover size="sm">
      <thead><tr><th>Event</th><th>Actor</th><th>When</th><th>Details</th></tr></thead>
      <tbody>
        {logs.map(log => (
          <tr key={log.id}>
            <td>{log.eventType}</td>
            <td>{log.actorId || 'System'} <small className="text-muted">({log.actorType})</small></td>
            <td>{new Date(log.occurredAt).toLocaleString()}</td>
            <td>
              <details>
                <summary>View</summary>
                {log.beforeJson && <pre className="small mb-1">Before: {formatJson(log.beforeJson)}</pre>}
                {log.afterJson && <pre className="small mb-1">After: {formatJson(log.afterJson)}</pre>}
                {log.metadataJson && <pre className="small mb-0">Metadata: {formatJson(log.metadataJson)}</pre>}
              </details>
            </td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
};

export default AuditLogList;