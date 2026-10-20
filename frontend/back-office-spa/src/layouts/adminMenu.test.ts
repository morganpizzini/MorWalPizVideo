import { describe, expect, it } from 'vitest';
import { permissions, getRoutePermissions } from '../authorization/permissions';
import { protectedRoutes } from '../router/routes';
import { adminMenuGroups } from './adminMenu';

describe('gallery back-office navigation', () => {
  it('exposes Galleries after Images and protects it with image permissions', () => {
    const contentItems = adminMenuGroups.find(group => group.label === 'Content')?.items ?? [];
    const imagesIndex = contentItems.findIndex(item => item.path === '/images');

    expect(contentItems[imagesIndex + 1]).toMatchObject({
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
});
