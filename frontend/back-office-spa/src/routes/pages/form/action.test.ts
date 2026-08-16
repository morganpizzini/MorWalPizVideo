import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPage, updatePage } from '@morwalpizvideo/services';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  createPage: vi.fn(),
  updatePage: vi.fn(),
}));

describe('page form action', () => {
  beforeEach(() => vi.clearAllMocks());

  it('sends the selected Draft status and HTML content when creating a page', async () => {
    vi.mocked(createPage).mockResolvedValue({ id: 'page-1' } as never);
    const formData = new FormData();
    formData.set('title', ' Draft page ');
    formData.set('url', 'draft-page');
    formData.set('status', '0');
    formData.set('content', '<div class="page-columns"><div class="page-column"><p>Draft</p></div></div>');
    formData.set('thumbnailUrl', ' thumbnail ');
    formData.set('videoId', ' video ');

    const result = await action({ request: new Request('http://localhost/pages/create', { method: 'POST', body: formData }), params: {} });

    expect(createPage).toHaveBeenCalledWith({
      title: 'Draft page',
      url: 'draft-page',
      status: 0,
      content: '<div class="page-columns"><div class="page-column"><p>Draft</p></div></div>',
      thumbnailUrl: 'thumbnail',
      videoId: 'video',
    });
    expect(result).toMatchObject({ data: { success: true }, init: { status: 201 } });
  });

  it('sends Published status when updating a page', async () => {
    vi.mocked(updatePage).mockResolvedValue({ id: 'page-1' } as never);
    const formData = new FormData();
    formData.set('title', 'Published page');
    formData.set('url', 'published-page');
    formData.set('status', '1');
    formData.set('content', '<p>Published</p>');

    await action({ request: new Request('http://localhost/pages/page-1/edit', { method: 'POST', body: formData }), params: { id: 'page-1' } });

    expect(updatePage).toHaveBeenCalledWith('page-1', expect.objectContaining({ status: 1, title: 'Published page' }));
  });

  it('rejects missing title and URL before calling the API', async () => {
    const formData = new FormData();
    formData.set('status', '1');

    const result = await action({ request: new Request('http://localhost/pages/create', { method: 'POST', body: formData }), params: {} });

    expect(createPage).not.toHaveBeenCalled();
    expect(result).toMatchObject({ data: { success: false, errors: { title: 'Title is required', url: 'URL is required' } } });
  });
});