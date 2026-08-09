export interface RbacUserSummary {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone: string;
  isActive: boolean;
  lastLogin?: string | null;
  groupIds: string[];
  groupCodes: string[];
  directPermissions: string[];
  effectivePermissions: string[];
  canAccessBackoffice: boolean;
  channelIds: string[];
}

export interface RbacGroup {
  id: string;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  permissions: string[];
  memberCount: number;
  members?: RbacGroupMember[];
}

export interface RbacGroupMember {
  id: string;
  username: string;
  email: string;
}

export function parsePermissions(input: string): string[] {
  return [...new Set(input.split(',').map(value => value.trim().toLowerCase()).filter(Boolean))];
}