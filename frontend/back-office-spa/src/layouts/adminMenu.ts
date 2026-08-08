import {
  Activity,
  CalendarDays,
  FileImage,
  FileText,
  Folder,
  KeyRound,
  Link,
  ListVideo,
  Settings,
  ShieldCheck,
  Tv,
  Users,
  type LucideIcon,
} from 'lucide-react';

export const BACKOFFICE_ACCESS = 'backoffice.access';
export const BACKOFFICE_MANAGE_ALL = 'backoffice.manageall';

export interface AdminMenuItem {
  label: string;
  path: string;
  permission: string;
  icon: LucideIcon;
}

export interface AdminMenuGroup {
  label: string;
  items: AdminMenuItem[];
}

export const adminMenuGroups: AdminMenuGroup[] = [
  { label: 'Overview', items: [{ label: 'Dashboard', path: '/', permission: BACKOFFICE_ACCESS, icon: Activity }] },
  {
    label: 'Content',
    items: [
      { label: 'Videos', path: '/videos', permission: 'videos.view', icon: ListVideo },
      { label: 'Channels', path: '/channels', permission: 'channels.view', icon: Tv },
      { label: 'Categories', path: '/categories', permission: 'categories.view', icon: Folder },
      { label: 'Images', path: '/images', permission: 'images.view', icon: FileImage },
      { label: 'Calendar', path: '/calendarevents', permission: 'calendar.view', icon: CalendarDays },
    ],
  },
  {
    label: 'Marketing',
    items: [
      { label: 'Short links', path: '/shortlinks', permission: 'shortlinks.view', icon: Link },
      { label: 'Query links', path: '/querylinks', permission: 'querylinks.view', icon: Link },
      { label: 'Forms', path: '/customforms', permission: 'forms.view', icon: FileText },
      { label: 'Insights', path: '/insights', permission: 'insights.view', icon: Activity },
    ],
  },
  {
    label: 'Administration',
    items: [
      { label: 'Users & access', path: '/rbac', permission: 'users.permissions.manage', icon: Users },
      { label: 'API keys', path: '/keys', permission: 'apikeys.view', icon: KeyRound },
      { label: 'Configuration', path: '/morwalpizconfigurations', permission: 'configurations.view', icon: Settings },
      { label: 'Diagnostics', path: '/diagnostics', permission: 'diagnostics.view', icon: ShieldCheck },
    ],
  },
];

export function canAccessMenuItem(item: AdminMenuItem, permissions: readonly string[]): boolean {
  return permissions.includes(BACKOFFICE_MANAGE_ALL) || permissions.includes(item.permission);
}