export interface AuditLog {
  id: string;
  eventType: string;
  entityType: string;
  entityId: string;
  actorId: string;
  actorType: string;
  occurredAt: string;
  beforeJson?: string | null;
  afterJson?: string | null;
  metadataJson?: string | null;
}