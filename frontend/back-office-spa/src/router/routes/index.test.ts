import { describe, expect, it } from 'vitest';
import { protectedRoutes } from './index';

describe('protectedRoutes', () => {
  it('includes profile route', () => {
    const hasProfile = protectedRoutes.some((route) => route.path === 'profile');
    expect(hasProfile).toBe(true);
  });
});
