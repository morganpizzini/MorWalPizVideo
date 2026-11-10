import {
  Activity,
  CalendarDays,
  FileImage,
  FileText,
  Menu,
  Folder,
  KeyRound,
  Link,
  ListVideo,
  Bell,
  Package,
  Settings,
  ShieldCheck,
  ShoppingBag,
  WandSparkles,
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
  id: string;
  label: string;
  items: AdminMenuItem[];
}

export const dashboardMenuItem: AdminMenuItem = {
  label: 'Dashboard',
  path: '/',
  permissions: [BACKOFFICE_ACCESS],
  icon: Activity,
};

export const adminMenuGroups: AdminMenuGroup[] = [
  {
    id: 'publishing',
    label: 'Publishing',
    items: [
      {
        label: 'Videos',
        path: '/videos',
        permissions: [permissions.videos.view, permissions.videos.manage],
        icon: ListVideo,
      },
      {
        label: 'Blog',
        path: '/blogposts',
        permissions: [permissions.pages.view, permissions.pages.manage],
        icon: FileText,
      },
      {
        label: 'Editorial production',
        path: '/editorialproductioncards',
        permissions: [
          permissions.editorialproductioncards.view,
          permissions.editorialproductioncards.manage,
        ],
        icon: WandSparkles,
      },
      {
        label: 'Calendar',
        path: '/calendarevents',
        permissions: [permissions.calendar.view, permissions.calendar.manage],
        icon: CalendarDays,
      },
      {
        label: 'Compilations',
        path: '/compilations',
        permissions: [permissions.compilations.view, permissions.compilations.manage],
        icon: ListVideo,
      },
      {
        label: 'Script Studio',
        path: '/script-studio',
        permissions: [permissions.scripts.studio],
        icon: WandSparkles,
      },
    ],
  },
  {
    id: 'media-library',
    label: 'Media library',
    items: [
      {
        label: 'Categories',
        path: '/categories',
        permissions: [permissions.categories.view, permissions.categories.manage],
        icon: Folder,
      },
      {
        label: 'Images',
        path: '/images',
        permissions: [permissions.images.view, permissions.images.manage],
        icon: FileImage,
      },
      {
        label: 'Galleries',
        path: '/galleries',
        permissions: [permissions.images.view, permissions.images.manage],
        icon: FileImage,
      },
    ],
  },
  {
    id: 'website',
    label: 'Website',
    items: [
      {
        label: 'Pages',
        path: '/pages',
        permissions: [permissions.pages.view, permissions.pages.manage],
        icon: FileText,
      },
      {
        label: 'Navigation',
        path: '/navigation',
        permissions: [permissions.navigation.view, permissions.navigation.manage],
        icon: Menu,
      },
      {
        label: 'QuickLinks',
        path: '/quicklinks',
        permissions: [permissions.quicklinks.view, permissions.quicklinks.manage],
        icon: Link,
      },
    ],
  },
  {
    id: 'audience',
    label: 'Audience',
    items: [
      {
        label: 'Forms',
        path: '/customforms',
        permissions: [permissions.forms.view, permissions.forms.manage],
        icon: FileText,
      },
      {
        label: 'Surveys',
        path: '/surveys',
        permissions: [permissions.forms.view, permissions.forms.manage],
        icon: FileText,
      },
      {
        label: 'Push notifications',
        path: '/push',
        permissions: [BACKOFFICE_ACCESS],
        icon: Bell,
      },
      {
        label: 'Blog interactions',
        path: '/blog-interactions',
        permissions: [permissions.blogInteractions.view, permissions.blogInteractions.moderate],
        icon: FileText,
      },
    ],
  },
  {
    id: 'growth-insights',
    label: 'Growth & insights',
    items: [
      {
        label: 'Insights',
        path: '/insights',
        permissions: [permissions.insights.view, permissions.insights.manage],
        icon: Activity,
      },
      {
        label: 'Short links',
        path: '/shortlinks',
        permissions: [permissions.shortlinks.view, permissions.shortlinks.manage],
        icon: Link,
      },
      {
        label: 'Query links',
        path: '/querylinks',
        permissions: [permissions.querylinks.view, permissions.querylinks.manage],
        icon: Link,
      },
      {
        label: 'Partners',
        path: '/partners',
        permissions: [permissions.partners.view, permissions.partners.manage],
        icon: Users,
      },
      {
        label: 'Campaigns',
        path: '/campaigns',
        permissions: [permissions.campaigns.view, permissions.campaigns.manage],
        icon: FileText,
      },
    ],
  },
  {
    id: 'catalog',
    label: 'Catalog',
    items: [
      {
        label: 'Product categories',
        path: '/productcategories',
        permissions: [permissions.productcategories.view, permissions.productcategories.manage],
        icon: Tags,
      },
      {
        label: 'Products',
        path: '/products',
        permissions: [permissions.products.view, permissions.products.manage],
        icon: ShoppingBag,
      },
      {
        label: 'Sponsors',
        path: '/sponsors',
        permissions: [permissions.sponsors.view, permissions.sponsors.manage],
        icon: Package,
      },
    ],
  },
  {
    id: 'administration',
    label: 'Administration',
    items: [
      {
        label: 'Channels',
        path: '/channels',
        permissions: [permissions.channels.admin],
        icon: Tv,
      },
      {
        label: 'Users & access',
        path: '/rbac',
        permissions: [
          permissions.users.view,
          permissions.users.manage,
          permissions.users.permissionsManage,
        ],
        icon: Users,
      },
      {
        label: 'Public accounts',
        path: '/public-accounts',
        permissions: [permissions.publicAccounts.view],
        icon: Users,
      },
      {
        label: 'API keys',
        path: '/keys',
        permissions: [permissions.apikeys.view, permissions.apikeys.manage],
        icon: KeyRound,
      },
      {
        label: 'Configuration',
        path: '/morwalpizconfigurations',
        permissions: [permissions.configurations.view, permissions.configurations.manage],
        icon: Settings,
      },
      {
        label: 'Diagnostics',
        path: '/diagnostics',
        permissions: [permissions.diagnostics.view],
        icon: ShieldCheck,
      },
      {
        label: 'AI workflows',
        path: '/ai-workflows',
        permissions: [
          permissions.aiPrompts.view,
          permissions.aiPrompts.edit,
          permissions.aiPrompts.evaluate,
          permissions.aiPrompts.activate,
        ],
        icon: ShieldCheck,
      },
    ],
  },
];

export function getAdminMenuGroupForPath(
  pathname: string,
  groups: readonly AdminMenuGroup[] = adminMenuGroups
): string | null {
  const normalizedPath = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname;
  let match: { groupId: string; pathLength: number } | null = null;

  for (const group of groups) {
    for (const item of group.items) {
      const itemPath = item.path.length > 1 ? item.path.replace(/\/+$/, '') : item.path;
      const matchesPath =
        itemPath === '/'
          ? normalizedPath === '/'
          : normalizedPath === itemPath || normalizedPath.startsWith(`${itemPath}/`);
      if (matchesPath && (!match || itemPath.length > match.pathLength)) {
        match = { groupId: group.id, pathLength: itemPath.length };
      }
    }
  }

  return match?.groupId ?? null;
}

export function canAccessMenuItem(item: AdminMenuItem, permissions: readonly string[]): boolean {
  return hasPermission(permissions, item.permissions);
}
