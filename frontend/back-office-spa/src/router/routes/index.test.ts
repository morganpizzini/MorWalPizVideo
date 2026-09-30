import { describe, expect, it } from 'vitest';
import { matchRoutes, type RouteObject } from 'react-router';
import { protectedRoutes } from './index';
import { getRoutePermissions, permissions } from '../../authorization/permissions';

describe('protectedRoutes', () => {
  it('includes profile route', () => {
    const hasProfile = protectedRoutes.some(route => route.path === 'profile');
    expect(hasProfile).toBe(true);
  });

  it('lazy-loads representative feature routes while retaining protected loaders', async () => {
    const profileRoute = protectedRoutes.find(route => route.path === 'profile');
    const videosRoute = protectedRoutes.find(route => route.path === 'videos');

    expect(profileRoute?.lazy).toBeTypeOf('function');
    expect(videosRoute?.children?.[0].lazy).toBeTypeOf('function');

    const resolvedVideoIndex = await (videosRoute!.children![0].lazy as () => Promise<unknown>)();
    expect((resolvedVideoIndex as { loader?: unknown }).loader).toBeTypeOf('function');
  });

  it('wires Calendar title routes to lazy loaders, forms and mutation actions', async () => {
    const title = '100% event / title';
    const calendarRoute = protectedRoutes.find(route => route.path === 'calendarevents')!;
    const resolve = (route: typeof calendarRoute) =>
      (route.lazy as () => Promise<{ Component?: unknown; loader?: unknown; action?: unknown }>)();
    const deletion = await resolve(calendarRoute);
    expect(deletion.action).toBeTypeOf('function');
    for (const suffix of [
      'create',
      encodeURIComponent(title),
      `${encodeURIComponent(title)}/edit`,
    ]) {
      const matches = matchRoutes(protectedRoutes as RouteObject[], `/calendarevents/${suffix}`)!;
      const leaf = matches.at(-1)!;
      const resolved = await resolve(leaf.route as typeof calendarRoute);
      expect(resolved.Component).toBeTypeOf('function');
      if (suffix !== 'create') {
        expect(leaf.params.id).toBe(title);
        expect(resolved.loader).toBeTypeOf('function');
      }
      if (suffix === 'create' || suffix.endsWith('/edit'))
        expect(resolved.action).toBeTypeOf('function');
    }
  });

  it('separates global channel administration from selected-channel self service', () => {
    expect(getRoutePermissions('channels', false)).toEqual([permissions.channels.admin]);
    expect(getRoutePermissions('channels/create', false)).toEqual([permissions.channels.admin]);
    expect(getRoutePermissions('my-channel', false)).toEqual([permissions.backoffice.access]);
  });

  it('protects newsletters with general backoffice access', () => {
    expect(getRoutePermissions('newsletters', false)).toEqual([permissions.backoffice.access]);
    expect(getRoutePermissions('newsletters', true)).toEqual([permissions.backoffice.access]);
  });
});
