import { describe, expect, it, vi } from 'vitest';
import loader from './loader';
import { ComposeUrl, endpoints, get } from '@morwalpizvideo/services';

vi.mock('@morwalpizvideo/services', () => ({
  ComposeUrl: vi.fn(
    (endpoint: string, values: Record<string, string>) => `${endpoint}/${values.videoId}`
  ),
  endpoints: {
    VIDEOS_DETAIL: '/videos',
    CATEGORIES: '/categories',
    CHANNELS_ACCESSIBLE: '/channels',
  },
  get: vi.fn(),
}));

describe('video detail loader', () => {
  it('exposes the loaded video title as the breadcrumb identifier', async () => {
    vi.mocked(get)
      .mockResolvedValueOnce({ id: 'video-1', title: 'Video caricato' })
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([]);

    const result = await loader({ params: { id: 'video-1' } } as never);

    expect(result.breadcrumbIdentifier).toBe('Video caricato');
    expect(ComposeUrl).toHaveBeenCalledWith(endpoints.VIDEOS_DETAIL, { videoId: 'video-1' });
  });
});
