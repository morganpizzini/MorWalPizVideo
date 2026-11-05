import { matchRoutes } from 'react-router';
import { describe, expect, it } from 'vitest';
import { routes } from './config';

describe('public gallery route', () => {
  it('resolves direct public utility links inside the shared application shell', () => {
    const cases = [
      ['/gallery/summer-trip', 'gallery/:slug'],
      ['/quick-links/community', 'quick-links/:url'],
      ['/channel-news/featured', 'channel-news/:idOrSlug'],
      ['/compilations/highlights', 'compilations/:compilationUrl'],
      ['/forms/contact', 'forms/:formUrl'],
      ['/survey/season', 'survey/:surveyUrl'],
    ] as const;

    for (const [url, routePath] of cases) {
      const matches = matchRoutes(routes, url);
      const routeMatch = matches?.find((match) => match.route.path === routePath);

      expect(matches?.some((match) => match.route.path === '/')).toBe(true);
      expect(routeMatch?.route).toEqual(
        expect.objectContaining({
          loader: expect.any(Function),
          element: expect.anything(),
        })
      );
    }

    const matches = matchRoutes(routes, '/gallery/summer-trip');
    expect(matches?.some((match) => match.route.path === 'gallery/:slug')).toBe(true);
  });
});
