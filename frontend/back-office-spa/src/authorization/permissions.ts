export const permissions = {
  backoffice: { access: 'backoffice.access', manageAll: 'backoffice.manageall' },
  users: { view: 'users.view', manage: 'users.manage', create: 'users.create', update: 'users.update', delete: 'users.delete', permissionsManage: 'users.permissions.manage' },
  videos: { view: 'videos.view', manage: 'videos.manage', create: 'videos.create', update: 'videos.update', delete: 'videos.delete', import: 'videos.import', translate: 'videos.translate', publish: 'videos.publish' },
  channels: resourcePermissions('channels'),
  channelnews: resourcePermissions('channelnews'),
  categories: resourcePermissions('categories'),
  images: resourcePermissions('images'),
  calendar: resourcePermissions('calendar'),
  shortlinks: resourcePermissions('shortlinks'),
  querylinks: resourcePermissions('querylinks'),
  quicklinks: resourcePermissions('quicklinks'),
  pages: resourcePermissions('pages'),
  navigation: { ...resourcePermissions('navigation'), update: 'navigation.update' },
  forms: resourcePermissions('forms'),
  insights: { ...resourcePermissions('insights'), scan: 'insights.scan' },
  apikeys: resourcePermissions('apikeys'),
  configurations: resourcePermissions('configurations'),
  productcategories: resourcePermissions('productcategories'),
  sponsors: resourcePermissions('sponsors'),
  products: resourcePermissions('products'),
  compilations: resourcePermissions('compilations'),
  diagnostics: { view: 'diagnostics.view' },
} as const;

function resourcePermissions(resource: string) {
  return {
    view: `${resource}.view`,
    manage: `${resource}.manage`,
    create: `${resource}.create`,
    update: `${resource}.update`,
    delete: `${resource}.delete`,
  } as const;
}

export function hasPermission(
  effectivePermissions: readonly string[],
  requiredPermissions: readonly string[]
): boolean {
  const normalized = new Set(effectivePermissions.map(permission => permission.toLowerCase()));
  return normalized.has(permissions.backoffice.manageAll)
    || requiredPermissions.some(permission => normalized.has(permission));
}

type StandardResource = ReturnType<typeof resourcePermissions>;

const routeResources: Record<string, StandardResource> = {
  calendarevents: permissions.calendar,
  querylinks: permissions.querylinks,
  quicklinks: permissions.quicklinks,
  pages: permissions.pages,
  navigation: permissions.navigation,
  shortlinks: permissions.shortlinks,
  channels: permissions.channels,
  channelnews: permissions.channelnews,
  categories: permissions.categories,
  images: permissions.images,
  morwalpizconfigurations: permissions.configurations,
  productcategories: permissions.productcategories,
  sponsors: permissions.sponsors,
  products: permissions.products,
  compilations: permissions.compilations,
  customforms: permissions.forms,
  insights: permissions.insights,
  keys: permissions.apikeys,
};

export function getRoutePermissions(path: string, action: boolean): readonly string[] {
  const segments = path.split('/').filter(Boolean);
  const module = segments[0] ?? '';

  if (!module) return [permissions.backoffice.access];
  if (module === 'profile') return [permissions.backoffice.access];
  if (module === 'diagnostics') return [permissions.diagnostics.view];
  if (module === 'rbac') {
    if (segments[1] === 'groups') return [permissions.users.permissionsManage];
    if (segments[1] === 'users' && segments.includes('create')) return [permissions.users.create, permissions.users.manage];
    if (segments[1] === 'users' && segments.includes('edit')) return [permissions.users.update, permissions.users.manage];
    return [permissions.users.view, permissions.users.manage, permissions.users.permissionsManage];
  }

  if (module === 'videos') {
    if (segments.includes('import')) return [permissions.videos.import, permissions.videos.manage];
    if (segments.includes('translate')) return [permissions.videos.translate, permissions.videos.manage];
    if (segments.includes('edit')) return [permissions.videos.update, permissions.videos.manage];
    if (action) return [permissions.videos.delete, permissions.videos.manage];
    return [permissions.videos.view, permissions.videos.manage];
  }

  const resource = routeResources[module];
  if (!resource) throw new Error(`Protected route module "${module}" has no permission mapping.`);
  if (module === 'navigation') return action ? [permissions.navigation.update, permissions.navigation.manage] : [permissions.navigation.view, permissions.navigation.manage];
  if (module === 'insights' && segments.includes('scan-news')) {
    return [permissions.insights.scan, permissions.insights.manage];
  }
  if (module === 'insights' && (segments.includes('comments') || segments.includes('analyze-comments'))) {
    return [permissions.insights.scan, permissions.insights.manage];
  }
  if (segments.includes('create')) return [resource.create, resource.manage];
  if (segments.includes('edit') || segments.some(segment => segment.startsWith(':') && action)) {
    return [resource.update, resource.manage];
  }
  if (action) return [resource.delete, resource.manage];
  return [resource.view, resource.manage];
}