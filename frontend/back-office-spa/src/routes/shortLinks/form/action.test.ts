import { beforeEach, describe, expect, it, vi } from 'vitest';
import { post, put } from '@morwalpizvideo/services';
import type { ActionFunctionArgs } from 'react-router';
import action from './action';

vi.mock('@morwalpizvideo/services', () => ({
  post: vi.fn(),
  put: vi.fn(),
  endpoints: {
    SHORTLINKS: '/api/shortlinks',
    SHORTLINKS_DETAIL: '/api/shortlinks/:querylinkId',
  },
  ComposeUrl: vi.fn((path: string) => path),
}));

const makeRequest = () => {
  const body = new URLSearchParams({
    target: 'https://example.test',
    linkType: '5',
    queryLinkIds: '[]',
  });
  return new Request('http://localhost/shortlinks/create', {
    method: 'POST',
    body,
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  });
};

const makeActionArgs = (params: ActionFunctionArgs['params']): ActionFunctionArgs => {
  const request = makeRequest();
  return {
    request,
    url: new URL(request.url),
    pattern: '/shortlinks/create',
    params,
    context: {},
  };
};

describe('short-link form action', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns an error when the API helper reports a failed create response', async () => {
    vi.mocked(post).mockResolvedValue({
      errors: ['An unexpected error occurred'],
      status: 500,
    });

    const result = await action(makeActionArgs({}));

    expect(result).toMatchObject({
      data: {
        success: false,
        errors: { generics: ['An unexpected error occurred'] },
      },
      init: { status: 500 },
    });
  });

  it('returns an error when the API helper reports a failed update response', async () => {
    vi.mocked(put).mockResolvedValue({ errors: ['Target is invalid'], status: 400 });

    const result = await action(makeActionArgs({ id: 'short-link' }));

    expect(result).toMatchObject({
      data: {
        success: false,
        errors: { generics: ['Target is invalid'] },
      },
      init: { status: 400 },
    });
  });

  it('preserves successful create responses', async () => {
    vi.mocked(post).mockResolvedValue('https://short.test/code');

    const result = await action(makeActionArgs({}));

    expect(result).toMatchObject({
      data: { success: true },
      init: { status: 201 },
    });
  });
});
