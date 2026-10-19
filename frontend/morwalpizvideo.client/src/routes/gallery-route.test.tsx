import { matchRoutes } from 'react-router';
import { describe, expect, it } from 'vitest';
import { routes } from './config';

describe('public gallery route', () => {
  it('resolves a direct gallery link outside the application shell', () => {
    const matches = matchRoutes(routes, '/gallery/summer-trip');
    const galleryMatch = matches?.find((match) => match.route.path === '/gallery/:slug');

    expect(galleryMatch?.route).toEqual(
      expect.objectContaining({
        loader: expect.any(Function),
        element: expect.anything(),
      })
    );
    expect(matches?.some((match) => match.route.path === '/')).toBe(false);
  });
});
