import { describe, expect, it } from 'vitest';
import { protectedRoutes } from './index';

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
});
