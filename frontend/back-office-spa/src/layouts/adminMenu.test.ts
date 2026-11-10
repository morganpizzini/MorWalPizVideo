import { describe, expect, it } from 'vitest';
import { permissions, getRoutePermissions } from '../authorization/permissions';
import { protectedRoutes } from '../router/routes';
import { adminMenuGroups, dashboardMenuItem, getAdminMenuGroupForPath } from './adminMenu';

describe('back-office navigation catalog', () => {
  it('retains every existing destination once and keeps each group manageable', () => {
    const items = adminMenuGroups.flatMap(group => group.items);
    const paths = [...items, dashboardMenuItem].map(item => item.path);

    expect(paths).toHaveLength(32);
    expect(new Set(paths).size).toBe(paths.length);
    expect(new Set(adminMenuGroups.map(group => group.id)).size).toBe(adminMenuGroups.length);
    expect(adminMenuGroups.every(group => group.items.length <= 7)).toBe(true);
    expect(paths).toEqual(
      expect.arrayContaining([
        '/',
        '/videos',
        '/blogposts',
        '/editorialproductioncards',
        '/calendarevents',
        '/compilations',
        '/script-studio',
        '/categories',
        '/images',
        '/galleries',
        '/pages',
        '/navigation',
        '/quicklinks',
        '/customforms',
        '/surveys',
        '/push',
        '/blog-interactions',
        '/insights',
        '/shortlinks',
        '/querylinks',
        '/partners',
        '/campaigns',
        '/productcategories',
        '/products',
        '/sponsors',
        '/channels',
        '/rbac',
        '/public-accounts',
        '/keys',
        '/morwalpizconfigurations',
        '/diagnostics',
        '/ai-workflows',
      ])
    );
  });

  it('keeps Galleries after Images and protects both with image permissions', () => {
    const mediaItems = adminMenuGroups.find(group => group.id === 'media-library')?.items ?? [];
    const imagesIndex = mediaItems.findIndex(item => item.path === '/images');

    expect(mediaItems[imagesIndex + 1]).toMatchObject({
      label: 'Galleries',
      path: '/galleries',
      permissions: [permissions.images.view, permissions.images.manage],
    });
    expect(protectedRoutes.some(route => route.path === 'galleries')).toBe(true);
    expect(getRoutePermissions('galleries', false)).toEqual([
      permissions.images.view,
      permissions.images.manage,
    ]);
    expect(getRoutePermissions('galleries', true)).toEqual([
      permissions.images.delete,
      permissions.images.manage,
    ]);
  });

  it('matches deep links to the module group without matching partial path names', () => {
    expect(getAdminMenuGroupForPath('/')).toBeNull();
    expect(getAdminMenuGroupForPath('/videos/123/edit')).toBe('publishing');
    expect(getAdminMenuGroupForPath('/blogposts/create')).toBe('publishing');
    expect(getAdminMenuGroupForPath('/videos-old')).toBeNull();
  });
});
