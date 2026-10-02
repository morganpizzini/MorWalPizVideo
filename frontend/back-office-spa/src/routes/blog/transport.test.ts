import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  createBlogPost,
  fetchBlogPosts,
  getPublicBlog,
  getPublicBlogPost,
  publishBlogPost,
  saveBlogPost,
  uploadBlogImage,
  resetCsrfToken,
  setSelectedChannelId,
  ApiResponseError,
} from '@morwalpizvideo/services';
import type { SaveBlogPost } from '@morwalpizvideo/models';

const payload: SaveBlogPost = {
  slug: 'article',
  revision: 7,
  draft: {
    title: 'Article',
    summary: '',
    author: '',
    tags: [],
    coverImageId: null,
    coverAlt: '',
    seoTitle: '',
    seoDescription: '',
    document: { version: 1, blocks: [] },
  },
};
afterEach(() => {
  vi.unstubAllGlobals();
  setSelectedChannelId(null);
  resetCsrfToken();
});

describe('blog HTTP contracts', () => {
  it('uses the selected admin channel and exact revision/body for every mutation', async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation((url: string) =>
        Promise.resolve(
          new Response(
            JSON.stringify(
              url.endsWith('/csrf') ? { token: 'csrf-token' } : { id: 'post-1', revision: 8 }
            ),
            { status: 200 }
          )
        )
      );
    vi.stubGlobal('fetch', fetchMock);
    setSelectedChannelId('channel-1');
    await fetchBlogPosts(2);
    await createBlogPost(payload);
    await saveBlogPost('post-1', payload);
    await publishBlogPost('post-1', 7, true);
    await publishBlogPost('post-1', 8, false);
    await uploadBlogImage(
      'post-1',
      9,
      new File(['image'], 'image.png', { type: 'image/png' }),
      'Image alt'
    );
    const requests = fetchMock.mock.calls.filter(([url]) => !url.endsWith('/csrf'));
    for (const [, options] of requests) {
      expect((options.headers as Headers).get('X-Channel-Id')).toBe('channel-1');
      expect(options.credentials).toBe('include');
      if (options.method !== 'GET')
        expect((options.headers as Headers).get('X-CSRF-TOKEN')).toBe('csrf-token');
    }
    expect(requests[0][0]).toContain('/api/blogposts?page=2');
    expect(JSON.parse(requests[2][1].body)).toEqual(payload);
    expect(requests[3][0]).toContain('/post-1/publish');
    expect(JSON.parse(requests[3][1].body)).toEqual({ revision: 7 });
    expect(requests[4][0]).toContain('/post-1/unpublish');
    const multipart = requests[5][1].body as FormData;
    expect(multipart.get('revision')).toBe('9');
    expect(multipart.get('altText')).toBe('Image alt');
    expect((requests[5][1].headers as Headers).has('Content-Type')).toBe(false);
  });
  it('does not leak the selected channel into public reads', async () => {
    const fetchMock = vi
      .fn()
      .mockImplementation(() => Promise.resolve(new Response('{}', { status: 200 })));
    vi.stubGlobal('fetch', fetchMock);
    setSelectedChannelId('private-channel');
    await getPublicBlog(3);
    await getPublicBlogPost('article');
    for (const [, options] of fetchMock.mock.calls) {
      expect((options.headers as Headers).has('X-Channel-Id')).toBe(false);
      expect(options.credentials).toBe('omit');
    }
    expect(fetchMock.mock.calls[0][0]).toContain('/api/blog?page=3');
    expect(fetchMock.mock.calls[1][0]).toContain('/api/blog/article');
  });
  it('surfaces revision conflicts instead of treating them as successful saves', async () => {
    setSelectedChannelId('channel-1');
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockImplementation((url: string) =>
          Promise.resolve(
            url.endsWith('/csrf')
              ? new Response(JSON.stringify({ token: 'csrf-token' }))
              : new Response('Post changed. Reload before saving.', { status: 409 })
          )
        )
    );
    await expect(saveBlogPost('post-1', payload)).rejects.toBeInstanceOf(ApiResponseError);
  });
});
