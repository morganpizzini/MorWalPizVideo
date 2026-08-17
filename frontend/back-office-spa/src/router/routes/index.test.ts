import { describe, expect, it } from 'vitest';
import { protectedRoutes } from './index';
import { getRoutePermissions, permissions } from '../../authorization/permissions';

describe('protectedRoutes', () => {
  it('includes profile route', () => {
    const hasProfile = protectedRoutes.some((route) => route.path === 'profile');
    expect(hasProfile).toBe(true);
  });

  it('lazy-loads representative feature routes while retaining protected loaders', async () => {
    const profileRoute = protectedRoutes.find((route) => route.path === 'profile');
    const videosRoute = protectedRoutes.find((route) => route.path === 'videos');

    expect(profileRoute?.lazy).toBeTypeOf('function');
    expect(videosRoute?.children?.[0].lazy).toBeTypeOf('function');

    const resolvedVideoIndex = await (videosRoute!.children![0].lazy as () => Promise<unknown>)();
    expect((resolvedVideoIndex as { loader?: unknown }).loader).toBeTypeOf('function');
  });

  it('separates global channel administration from selected-channel self service', () => {
    expect(getRoutePermissions('channels', false)).toEqual([permissions.channels.admin]);
    expect(getRoutePermissions('channels/create', false)).toEqual([permissions.channels.admin]);
    expect(getRoutePermissions('my-channel', false)).toEqual([permissions.backoffice.access]);
  });
});
