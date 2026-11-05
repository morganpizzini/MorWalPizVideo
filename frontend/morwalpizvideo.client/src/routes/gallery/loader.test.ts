import { ApiResponseError } from '@morwalpizvideo/services';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import loader, { GALLERY_UNAVAILABLE_MESSAGE } from './loader';
import { fetchPublicGallery } from '../../services/gallery';

vi.mock('../../services/gallery', () => ({
  fetchPublicGallery: vi.fn(),
}));

const loaderArgs: Parameters<typeof loader>[0] = {
  request: new Request('https://public.test/gallery/expired'),
  url: new URL('https://public.test/gallery/expired'),
  params: { slug: 'expired' },
  pattern: '/gallery/:slug',
  context: {},
};

beforeEach(() => vi.clearAllMocks());

describe('public gallery loader', () => {
  it('maps API 404 responses to the standard unavailable message', async () => {
    vi.mocked(fetchPublicGallery).mockRejectedValue(
      new ApiResponseError({ status: 404, errors: ['Not found'] })
    );

    await expect(loader(loaderArgs)).rejects.toMatchObject({
      status: 404,
      statusText: GALLERY_UNAVAILABLE_MESSAGE,
    });
    expect(fetchPublicGallery).toHaveBeenCalledWith('expired');
  });

  it('preserves non-404 API errors', async () => {
    const error = new ApiResponseError({ status: 500, errors: ['Server error'] });
    vi.mocked(fetchPublicGallery).mockRejectedValue(error);

    await expect(loader(loaderArgs)).rejects.toBe(error);
  });
});
