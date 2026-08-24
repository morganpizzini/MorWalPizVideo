import { beforeEach, describe, expect, it, vi } from 'vitest';
import action from '../action';

const mockPost = vi.hoisted(() => vi.fn());

vi.mock('@morwalpizvideo/services', () => ({
  post: mockPost,
  put: vi.fn(),
  endpoints: {
    VIDEOS_VIDEO_REFS: 'api/videos/{videoId}/video-refs',
    VIDEOS_DETAIL: 'api/videos/{videoId}',
  },
  ComposeUrl: (url: string, replacements: Record<string, string>) =>
    url.replace('{videoId}', replacements.videoId),
}));

describe('video edit action', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sends only youtubeId and category IDs to the add-reference endpoint', async () => {
    mockPost.mockResolvedValue({
      youtubeId: 'new-video',
      categories: [{ id: 'category-1', title: 'News' }],
      channelIds: ['channel-1'],
      title: '',
      description: '',
      publishedAt: '',
      creationDateTime: '2026-01-01T00:00:00.000Z',
    });
    const formData = new FormData();
    formData.set('_intent', 'addVideoReference');
    formData.set('youtubeId', ' new-video ');
    formData.set('categories', JSON.stringify(['category-1']));

    await action({
      request: new Request('http://localhost/videos/match-1/edit', {
        method: 'POST',
        body: formData,
      }),
      params: { id: 'match-1' },
    } as never);

    expect(mockPost).toHaveBeenCalledWith(
      'api/videos/match-1/video-refs',
      { youtubeId: 'new-video', categories: ['category-1'] }
    );
  });
});
