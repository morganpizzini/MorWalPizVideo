import {
  Activity,
  CalendarDays,
  FileImage,
  FileText,
  Folder,
  KeyRound,
  Link,
  ListVideo,
  Package,
  Settings,
  ShieldCheck,
  ShoppingBag,
  Tags,
  Tv,
  Users,
  type LucideIcon,
} from 'lucide-react';
import { hasPermission, permissions } from '../authorization/permissions';

export const BACKOFFICE_ACCESS = permissions.backoffice.access;
export const BACKOFFICE_MANAGE_ALL = permissions.backoffice.manageAll;

export interface AdminMenuItem {
  label: string;
  path: string;
  permissions: readonly string[];
  icon: LucideIcon;
}

export interface AdminMenuGroup {
  label: string;
  items: AdminMenuItem[];
}

export const adminMenuGroups: AdminMenuGroup[] = [
  { label: 'Overview', items: [{ label: 'Dashboard', path: '/', permissions: [BACKOFFICE_ACCESS], icon: Activity }] },
  {
    label: 'Content',
    items: [
      { label: 'Videos', path: '/videos', permissions: [permissions.videos.view, permissions.videos.manage], icon: ListVideo },
      { label: 'Channels', path: '/channels', permissions: [permissions.channels.view, permissions.channels.manage], icon: Tv },
      { label: 'Categories', path: '/categories', permissions: [permissions.categories.view, permissions.categories.manage], icon: Folder },
      { label: 'Images', path: '/images', permissions: [permissions.images.view, permissions.images.manage], icon: FileImage },
      { label: 'Calendar', path: '/calendarevents', permissions: [permissions.calendar.view, permissions.calendar.manage], icon: CalendarDays },
      { label: 'Compilations', path: '/compilations', permissions: [permissions.compilations.view, permissions.compilations.manage], icon: ListVideo },
    ],
  },
  {
    label: 'Catalog',
    items: [
      { label: 'Product categories', path: '/productcategories', permissions: [permissions.productcategories.view, permissions.productcategories.manage], icon: Tags },
      { label: 'Sponsors', path: '/sponsors', permissions: [permissions.sponsors.view, permissions.sponsors.manage], icon: Package },
      { label: 'Products', path: '/products', permissions: [permissions.products.view, permissions.products.manage], icon: ShoppingBag },
    ],
  },
  {
    label: 'Marketing',
    items: [
      { label: 'Short links', path: '/shortlinks', permissions: [permissions.shortlinks.view, permissions.shortlinks.manage], icon: Link },
      { label: 'Query links', path: '/querylinks', permissions: [permissions.querylinks.view, permissions.querylinks.manage], icon: Link },
      { label: 'QuickLinks', path: '/quicklinks', permissions: [permissions.quicklinks.view, permissions.quicklinks.manage], icon: Link },
      { label: 'Forms', path: '/customforms', permissions: [permissions.forms.view, permissions.forms.manage], icon: FileText },
      { label: 'Insights', path: '/insights', permissions: [permissions.insights.view, permissions.insights.manage], icon: Activity },
    ],
  },
  {
    label: 'Administration',
    items: [
      { label: 'Users & access', path: '/rbac', permissions: [permissions.users.view, permissions.users.manage, permissions.users.permissionsManage], icon: Users },
      { label: 'API keys', path: '/keys', permissions: [permissions.apikeys.view, permissions.apikeys.manage], icon: KeyRound },
      { label: 'Configuration', path: '/morwalpizconfigurations', permissions: [permissions.configurations.view, permissions.configurations.manage], icon: Settings },
      { label: 'Diagnostics', path: '/diagnostics', permissions: [permissions.diagnostics.view], icon: ShieldCheck },
    ],
  },
];

export function canAccessMenuItem(item: AdminMenuItem, permissions: readonly string[]): boolean {
  return hasPermission(permissions, item.permissions);
}