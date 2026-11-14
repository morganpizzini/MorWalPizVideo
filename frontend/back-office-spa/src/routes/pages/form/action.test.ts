import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPage, updatePage } from '@morwalpizvideo/services';
import { createFormRequest } from '@/test/request-fixtures';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  createPage: vi.fn(),
  updatePage: vi.fn(),
}));

describe('page form action', () => {
  beforeEach(() => vi.clearAllMocks());

  it('sends the selected Draft status and HTML content when creating a page', async () => {
    vi.mocked(createPage).mockResolvedValue({ id: 'page-1' } as never);
    const result = await action({
      request: createFormRequest('http://localhost/pages/create', {
        title: ' Draft page ',
        url: 'draft-page',
        status: '0',
        content: '<div class="page-columns"><div class="page-column"><p>Draft</p></div></div>',
        thumbnailUrl: ' thumbnail ',
        videoId: ' video ',
      }),
      params: {},
    });

    expect(createPage).toHaveBeenCalledWith({
      title: 'Draft page',
      url: 'draft-page',
      status: 0,
      content: '<div class="page-columns"><div class="page-column"><p>Draft</p></div></div>',
      thumbnailUrl: 'thumbnail',
      videoId: 'video',
      customFormUrl: null,
    });
    expect(result).toMatchObject({ data: { success: true }, init: { status: 201 } });
  });

  it('sends Published status when updating a page', async () => {
    vi.mocked(updatePage).mockResolvedValue({ id: 'page-1' } as never);
    await action({
      request: createFormRequest('http://localhost/pages/page-1/edit', {
        title: 'Published page',
        url: 'published-page',
        status: '1',
        content: '<p>Published</p>',
      }),
      params: { id: 'page-1' },
    });

    expect(updatePage).toHaveBeenCalledWith(
      'page-1',
      expect.objectContaining({ status: 1, title: 'Published page' })
    );
  });

  it('rejects missing title and URL before calling the API', async () => {
    const result = await action({
      request: createFormRequest('http://localhost/pages/create', { status: '1' }),
      params: {},
    });

    expect(createPage).not.toHaveBeenCalled();
    expect(result).toMatchObject({
      data: { success: false, errors: { title: 'Title is required', url: 'URL is required' } },
    });
  });
});
