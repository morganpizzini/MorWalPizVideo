import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createSponsorWithImage, updateSponsorWithImage } from '@morwalpizvideo/services';
import type { ActionFunctionArgs } from 'react-router';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  createSponsorWithImage: vi.fn(),
  updateSponsorWithImage: vi.fn(),
}));

interface SponsorServiceResult {
  errors?: string[];
  status?: number;
}

const makeRequest = () => {
  const formData = new FormData();
  formData.set('title', 'Sponsor');
  formData.set('url', 'https://example.test');
  return new Request('http://localhost/sponsors/create', { method: 'POST', body: formData });
};

const makeActionArgs = (params: ActionFunctionArgs['params']): ActionFunctionArgs => {
  const request = makeRequest();
  return {
    request,
    url: new URL(request.url),
    pattern: '/sponsors/:sponsorId/edit',
    params,
    context: {},
  };
};

describe('sponsor form action', () => {
  beforeEach(() => vi.clearAllMocks());

  it('treats an empty successful response as success', async () => {
    const serviceResult: SponsorServiceResult = {};
    vi.mocked(createSponsorWithImage).mockResolvedValue(serviceResult);

    await expect(action(makeActionArgs({}))).resolves.toEqual({
      success: true,
    });
  });

  it('normalizes API error envelopes into generic errors', async () => {
    const serviceResult: SponsorServiceResult = {
      errors: ['Title is invalid'],
      status: 400,
    };
    vi.mocked(createSponsorWithImage).mockResolvedValue(serviceResult);

    await expect(action(makeActionArgs({}))).resolves.toEqual({
      success: false,
      errors: { generics: ['Title is invalid'] },
    });
  });

  it('uses sponsorId for updates and preserves exception handling', async () => {
    vi.mocked(updateSponsorWithImage).mockRejectedValue(new Error('Network failure'));

    await expect(action(makeActionArgs({ sponsorId: 'sponsor-1' }))).resolves.toEqual({
      success: false,
      errors: { generics: ['Network failure'] },
    });
    expect(updateSponsorWithImage).toHaveBeenCalledWith('sponsor-1', expect.anything());
  });
});
