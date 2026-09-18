import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createSponsorWithImage, updateSponsorWithImage } from '@morwalpizvideo/services';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  createSponsorWithImage: vi.fn(),
  updateSponsorWithImage: vi.fn(),
}));

const makeRequest = () => {
  const formData = new FormData();
  formData.set('title', 'Sponsor');
  formData.set('url', 'https://example.test');
  return new Request('http://localhost/sponsors/create', { method: 'POST', body: formData });
};

describe('sponsor form action', () => {
  beforeEach(() => vi.clearAllMocks());

  it('treats an empty successful response as success', async () => {
    vi.mocked(createSponsorWithImage).mockResolvedValue({} as never);

    await expect(action({ request: makeRequest(), params: {} })).resolves.toEqual({
      success: true,
    });
  });

  it('normalizes API error envelopes into generic errors', async () => {
    vi.mocked(createSponsorWithImage).mockResolvedValue({
      errors: ['Title is invalid'],
      status: 400,
    } as never);

    await expect(action({ request: makeRequest(), params: {} })).resolves.toEqual({
      success: false,
      errors: { generics: ['Title is invalid'] },
    });
  });

  it('uses sponsorId for updates and preserves exception handling', async () => {
    vi.mocked(updateSponsorWithImage).mockRejectedValue(new Error('Network failure'));

    await expect(
      action({ request: makeRequest(), params: { sponsorId: 'sponsor-1' } })
    ).resolves.toEqual({
      success: false,
      errors: { generics: ['Network failure'] },
    });
    expect(updateSponsorWithImage).toHaveBeenCalledTimes(1);
    expect(updateSponsorWithImage.mock.calls[0][0]).toBe('sponsor-1');
  });
});
