import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getMatches } from '@services/matches';
import { getConfiguration } from '@services/stream';
import { getPublicSurveys } from '@services/surveys';
import { getSponsors } from '@services/sponsors';
import { getPublicBlog, getPublicChannelNews } from '@morwalpizvideo/services';
import loader, { homeShouldRevalidate } from './loader';

vi.mock('@services/matches', () => ({ getMatches: vi.fn() }));
vi.mock('@services/stream', () => ({ getConfiguration: vi.fn() }));
vi.mock('@services/surveys', () => ({ getPublicSurveys: vi.fn() }));
vi.mock('@services/sponsors', () => ({ getSponsors: vi.fn() }));
vi.mock('@morwalpizvideo/services', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  getPublicBlog: vi.fn(),
  getPublicChannelNews: vi.fn(),
}));

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(getMatches).mockResolvedValue({ data: [{ contentId: 'video' }], count: 1 });
  vi.mocked(getConfiguration).mockResolvedValue({});
  vi.mocked(getPublicSurveys).mockResolvedValue([]);
  vi.mocked(getPublicChannelNews).mockResolvedValue([]);
  vi.mocked(getSponsors).mockResolvedValue([]);
});

describe('editorial homepage loader', () => {
  it('filters locally while preserving explicit retry and route revalidation', () => {
    const args = { currentUrl: new URL('https://public.test/'), nextUrl: new URL('https://public.test/?type=article'), defaultShouldRevalidate: true } as Parameters<typeof homeShouldRevalidate>[0];
    expect(homeShouldRevalidate(args)).toBe(false);
    expect(homeShouldRevalidate({ ...args, nextUrl: args.currentUrl })).toBe(true);
    expect(homeShouldRevalidate({ ...args, nextUrl: new URL('https://public.test/blog') })).toBe(true);
  });

  it('combines existing matches with published articles', async () => {
    vi.mocked(getPublicBlog).mockResolvedValue({
      items: [{ slug: 'published', title: 'Published article', summary: '', author: '', tags: [], coverUrl: null, coverAlt: '', publishedAt: '2026-10-01' }],
      page: 1,
      pageSize: 12,
      hasMore: false,
    });
    const data = await loader();
    expect(getMatches).toHaveBeenCalledWith(true);
    expect(getPublicBlog).toHaveBeenCalledWith(1);
    expect(data.matches).toEqual([{ contentId: 'video' }]);
    expect(data.articles[0].slug).toBe('published');
    expect(data.error).toBe(false);
    expect(data.articlesUnavailable).toBe(false);
  });

  it('keeps videos available when the optional article endpoint fails', async () => {
    vi.mocked(getPublicBlog).mockRejectedValue(new Error('Unavailable'));
    const data = await loader();
    expect(data.matches).toHaveLength(1);
    expect(data.articles).toEqual([]);
    expect(data.articlesUnavailable).toBe(true);
    expect(data.error).toBe(false);
  });

  it('preserves the retry state when the primary content request fails', async () => {
    vi.mocked(getPublicBlog).mockRejectedValue(new Error('Unavailable'));
    vi.mocked(getMatches).mockRejectedValue(new Error('Unavailable'));
    expect(await loader()).toMatchObject({ matches: [], articles: [], error: true });
  });

  it('handles transport error envelopes instead of passing them to components', async () => {
    vi.mocked(getPublicBlog).mockRejectedValue(new Error('Unavailable'));
    vi.mocked(getMatches).mockResolvedValue({ status: 500, errors: ['Unavailable'] });
    expect(await loader()).toMatchObject({ matches: [], surveys: [], sponsors: [], error: true });
  });
});