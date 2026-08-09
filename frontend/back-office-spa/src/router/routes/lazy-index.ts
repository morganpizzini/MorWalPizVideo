import { Outlet } from 'react-router';
import Home from '../../routes/Home';
import { createErrorElement, createRouteGroup } from '../utils';
import type { RouteConfig } from '../types';
import { lazyAction, lazyRoute } from '../lazyRoute';
import { getRoutePermissions } from '../../authorization/permissions';
import { withActionPermission, withPermission } from '../guards';

const feature = (path: string, importer: Parameters<typeof lazyRoute>[0], options: RouteConfig = {}): RouteConfig => ({
  path,
  ...options,
  ...lazyRoute(importer),
});

const indexFeature = (path: string, importer: Parameters<typeof lazyRoute>[0]): RouteConfig => ({
  index: true,
  ...feature(path, importer),
}) as RouteConfig;

const group = (path: string, actionImporter: Parameters<typeof lazyAction>[0], children: RouteConfig[]): RouteConfig => ({
  ...createRouteGroup(path, { children }),
  ...lazyAction(actionImporter),
});

const actionFeature = (path: string, importer: Parameters<typeof lazyAction>[0]): RouteConfig => ({
  path,
  ...lazyAction(importer),
});

const routeDefinitions: RouteConfig[] = [
  { index: true, path: '', Component: Home, errorElement: createErrorElement() },
  feature('diagnostics', () => import('../../routes/diagnostics'), { errorElement: createErrorElement() }),
  feature('profile', () => import('../../routes/profile'), { errorElement: createErrorElement() }),
  {
    path: 'rbac', Component: Outlet, errorElement: createErrorElement(), children: [
      indexFeature('', () => import('../../routes/rbac')),
      feature('users', () => import('../../routes/rbac/UsersPage')),
      feature('users/create', () => import('../../routes/rbac/UserCreatePage')),
      feature('users/:id', () => import('../../routes/rbac/UsersDetailPage')),
      feature('users/:id/edit', () => import('../../routes/rbac/UserEditPage')),
      feature('groups', () => import('../../routes/rbac/GroupsPage')),
      feature('groups/create', () => import('../../routes/rbac/GroupCreatePage')),
      feature('groups/:id', () => import('../../routes/rbac/GroupDetailPage')),
      feature('groups/:id/edit', () => import('../../routes/rbac/GroupEditPage')),
    ],
  },
  group('calendarevents', () => import('../../routes/calendarEvents/index'), [
    indexFeature('', () => import('../../routes/calendarEvents/index')),
    feature('create', () => import('../../routes/calendarEvents/create')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/calendarEvents/detail')), feature('edit', () => import('../../routes/calendarEvents/edit'))] },
  ]),
  group('querylinks', () => import('../../routes/queryLinks/index'), [
    indexFeature('', () => import('../../routes/queryLinks/index')),
    feature('create', () => import('../../routes/queryLinks/create')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/queryLinks/detail')), feature('edit', () => import('../../routes/queryLinks/edit'))] },
  ]),
  group('shortlinks', () => import('../../routes/shortLinks/index'), [
    indexFeature('', () => import('../../routes/shortLinks/index')),
    feature('create', () => import('../../routes/shortLinks/form')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/shortLinks/detail')), feature('edit', () => import('../../routes/shortLinks/form'))] },
  ]),
  group('channels', () => import('../../routes/channels/index'), [
    indexFeature('', () => import('../../routes/channels/index')),
    feature('create', () => import('../../routes/channels/form')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/channels/detail')), feature('edit', () => import('../../routes/channels/form'))] },
  ]),
  group('categories', () => import('../../routes/categories/index'), [
    indexFeature('', () => import('../../routes/categories/index')),
    feature('create', () => import('../../routes/categories/create')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/categories/detail')), feature('edit', () => import('../../routes/categories/edit'))] },
  ]),
  {
    path: 'videos', Component: Outlet, errorElement: createErrorElement(), children: [
      indexFeature('', () => import('../../routes/videos/index')),
      feature('import', () => import('../../routes/videos/import')),
      feature('translate', () => import('../../routes/videos/translate')),
      { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/videos/detail')), feature('edit', () => import('../../routes/videos/edit'))] },
    ],
  },
  {
    path: 'images', Component: Outlet, errorElement: createErrorElement(), children: [
      indexFeature('', () => import('../../routes/images/index')),
      feature('upload', () => import('../../routes/images/upload')),
      feature('upload-multiple', () => import('../../routes/images/upload-multiple')),
    ],
  },
  group('morwalpizconfigurations', () => import('../../routes/morwalpizconfigurations/index'), [
    indexFeature('', () => import('../../routes/morwalpizconfigurations/index')),
    feature('create', () => import('../../routes/morwalpizconfigurations/create')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/morwalpizconfigurations/detail')), feature('edit', () => import('../../routes/morwalpizconfigurations/edit'))] },
  ]),
  group('productcategories', () => import('../../routes/productCategories/index'), [
    indexFeature('', () => import('../../routes/productCategories/index')),
    feature('create', () => import('../../routes/productCategories/form')),
    feature(':categoryId/edit', () => import('../../routes/productCategories/form')),
  ]),
  group('sponsors', () => import('../../routes/sponsors/index'), [
    indexFeature('', () => import('../../routes/sponsors/index')),
    feature('create', () => import('../../routes/sponsors/create')),
    feature(':sponsorId/edit', () => import('../../routes/sponsors/edit')),
  ]),
  group('products', () => import('../../routes/products/index'), [
    indexFeature('', () => import('../../routes/products/index')),
    feature('create', () => import('../../routes/products/form')),
    { path: ':productId', Component: Outlet, children: [indexFeature('', () => import('../../routes/products/detail')), feature('edit', () => import('../../routes/products/form'))] },
  ]),
  group('compilations', () => import('../../routes/compilations/index'), [
    indexFeature('', () => import('../../routes/compilations/index')),
    feature('create', () => import('../../routes/compilations/form')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/compilations/detail')), feature('edit', () => import('../../routes/compilations/form'))] },
  ]),
  group('customforms', () => import('../../routes/customForms/index'), [
    indexFeature('', () => import('../../routes/customForms/index')),
    feature('create', () => import('../../routes/customForms/form')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/customForms/detail')), feature('edit', () => import('../../routes/customForms/form'))] },
  ]),
  group('insights', () => import('../../routes/insights/index'), [
    indexFeature('', () => import('../../routes/insights/index')),
    feature('create', () => import('../../routes/insights/form')),
    feature('news/:newsId', () => import('../../routes/insights/news')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/insights/detail')), feature('edit', () => import('../../routes/insights/form')), actionFeature('scan-news', () => import('../../routes/insights/scan-news/action'))] },
  ]),
  group('keys', () => import('../../routes/apiKeys/index'), [
    indexFeature('', () => import('../../routes/apiKeys/index')),
    feature('create', () => import('../../routes/apiKeys/form')),
    { path: ':id', Component: Outlet, children: [indexFeature('', () => import('../../routes/apiKeys/detail')), feature('edit', () => import('../../routes/apiKeys/form'))] },
  ]),
];

function protectRoute(route: RouteConfig, parentPath = ''): RouteConfig {
  const currentPath = [parentPath, route.path].filter(Boolean).join('/');
  const loaderPermissions = getRoutePermissions(currentPath, false);
  const actionPermissions = getRoutePermissions(currentPath, true);
  const lazy = typeof route.lazy === 'function' ? route.lazy : undefined;

  return {
    ...route,
    loader: typeof route.loader === 'function' ? withPermission(loaderPermissions, route.loader) : undefined,
    action: typeof route.action === 'function' ? withActionPermission(actionPermissions, route.action) : undefined,
    lazy: lazy ? async () => {
      const resolved = await lazy();
      return {
        ...resolved,
        loader: typeof resolved.loader === 'function' ? withPermission(loaderPermissions, resolved.loader) : undefined,
        action: typeof resolved.action === 'function' ? withActionPermission(actionPermissions, resolved.action) : undefined,
      };
    } : undefined,
    children: route.children?.map(child => protectRoute(child, currentPath)),
  } as RouteConfig;
}

export const protectedRoutes = routeDefinitions.map(route => protectRoute(route));
