import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { matchRoutes, type RouteObject } from 'react-router-dom';
import { getShootingItaQuickLinks } from '../../services/quickLinks';
import { quickLinkLoader } from './loader';

vi.mock('../../services/quickLinks', () => ({ getShootingItaQuickLinks: vi.fn() }));
vi.mock('react19-google-recaptcha-v3', () => ({ useGoogleReCaptcha: vi.fn() }));

beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal('localStorage', {
    getItem: () => null, setItem: vi.fn(), removeItem: vi.fn(), clear: vi.fn(),
    key: () => null, length: 0,
  } satisfies Storage);
});
afterEach(() => vi.unstubAllGlobals());

describe('QuickLinks route compatibility', () => {
  it.each([
    ['/quick-links/community', { url: 'community' }],
    ['/quick-link/community', { 'custom-linktree': 'community' }],
  ])('loads the same page at %s', async (path, params) => {
    const { routes } = await import('../config');
    const matches = matchRoutes<RouteObject>(routes, path);
    expect(matches?.at(-1)?.route.loader).toBe(quickLinkLoader);
    vi.mocked(getShootingItaQuickLinks).mockResolvedValue({ title: 'Community', url: 'community', links: [] });
    await quickLinkLoader({
      params, request: new Request(`https://example.com${path}`), context: {},
      url: new URL(`https://example.com${path}`), pattern: matches!.at(-1)!.route.path!,
    });
    expect(getShootingItaQuickLinks).toHaveBeenCalledWith('community');
  });
});
