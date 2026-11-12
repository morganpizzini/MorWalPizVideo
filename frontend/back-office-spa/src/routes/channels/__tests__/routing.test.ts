import { beforeEach, describe, expect, it, vi } from 'vitest';
import { get, post, put, Delete, getSelectedChannelId } from '@morwalpizvideo/services';
import detailLoader from '../detail/loader';
import formLoader from '../form/loader';
import formAction from '../form/action';
import indexLoader from '../index/loader';
import indexAction from '../index/action';
import selfLoader from '../self/loader';
import selfEditAction from '../self/edit/action';

vi.mock('@morwalpizvideo/services', async importOriginal => ({
  ...(await importOriginal<typeof import('@morwalpizvideo/services')>()),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  Delete: vi.fn(),
  getSelectedChannelId: vi.fn(),
  endpoints: {
    CHANNELS: '/api/channels',
    CHANNELS_DETAIL: '/api/channels/:channelId',
  },
  ComposeUrl: (endpoint: string, values: Record<string, string>) =>
    endpoint.replace(':channelId', values.channelId),
}));

const scopedNotFound = {
  errors: ['The selected channel was not found or is not accessible'],
  status: 404,
  channelContextError: true,
};

const scopedBadRequest = {
  errors: ['X-Channel-Id header is required'],
  status: 400,
  channelContextError: true,
};

function formRequest(values: Record<string, string>) {
  return new Request('http://localhost/channels', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams(values).toString(),
  });
}

function responseStatus(result: unknown): number | undefined {
  if (result instanceof Response) {
    return result.status;
  }

  if (typeof result === 'object' && result !== null && 'init' in result) {
    const init = (result as { init?: ResponseInit }).init;
    return init?.status;
  }

  return undefined;
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('channel route error handling', () => {
  it.each([undefined, 'channel-one'])(
    'forwards normalized public origin on create/edit (%s)',
    async id => {
      vi.mocked(post).mockResolvedValue({});
      vi.mocked(put).mockResolvedValue({});
      const result = await formAction({
        request: formRequest({
          channelName: 'Channel',
          yTChannelId: 'UCONE',
          publicBaseUrl: ' https://EXAMPLE.com:8443/ ',
        }),
        params: { id },
      } as never);
      expect(responseStatus(result)).toBe(id ? 200 : 201);
      expect(id ? put : post).toHaveBeenCalledWith(
        id ? '/api/channels/channel-one' : '/api/channels',
        expect.objectContaining({ publicBaseUrl: 'https://example.com:8443' })
      );
    }
  );

  it('clears public origin through the shared self-service action', async () => {
    vi.mocked(getSelectedChannelId).mockReturnValue('channel-one');
    vi.mocked(put).mockResolvedValue({});
    await selfEditAction({ request: formRequest({ channelName: 'Channel', publicBaseUrl: ' ' }) });
    expect(put).toHaveBeenCalledWith(
      '/api/channels/channel-one',
      expect.objectContaining({ publicBaseUrl: '' })
    );
  });

  it.each([
    '/relative',
    '//example.com',
    'javascript:alert(1)',
    'ftp://example.com',
    'https://user:password@example.com',
    'https://@example.com',
    'https://example.com/path',
    'https://example.com/../',
    'https://example.com?',
    'https://example.com#',
    'https://example.com\\path',
  ])('rejects invalid public origin without calling the API: %s', async publicBaseUrl => {
    const result = await formAction({
      request: formRequest({ channelName: 'Channel', publicBaseUrl }),
      params: { id: 'channel-one' },
    } as never);
    expect(responseStatus(result)).toBe(400);
    expect(result).toMatchObject({
      data: { success: false, errors: { publicBaseUrl: expect.any(String) } },
    });
    expect(put).not.toHaveBeenCalled();
    expect(post).not.toHaveBeenCalled();
  });

  it('throws a router 404 instead of returning an API error as a Channel', async () => {
    vi.mocked(get).mockResolvedValue(scopedNotFound as never);

    await expect(
      detailLoader({ params: { id: 'missing-channel' } } as never)
    ).rejects.toMatchObject({ status: 404 });
    await expect(formLoader({ params: { id: 'missing-channel' } })).rejects.toMatchObject({
      status: 404,
    });
  });

  it('returns scoped 400/404 statuses and messages from CRUD actions', async () => {
    vi.mocked(put).mockResolvedValue(scopedNotFound as never);
    const updateResult = await formAction({
      request: formRequest({ channelName: 'Updated' }),
      params: { id: 'channel-one' },
    } as never);

    vi.mocked(Delete).mockResolvedValue(scopedBadRequest as never);
    const deleteResult = await indexAction({ request: formRequest({ id: 'channel-one' }) });

    expect(responseStatus(updateResult)).toBe(404);
    expect(responseStatus(deleteResult)).toBe(400);
  });

  it('throws a router 400 when the accessible-channel list returns an error envelope', async () => {
    vi.mocked(get).mockResolvedValue(scopedBadRequest as never);

    await expect(indexLoader()).rejects.toMatchObject({ status: 400 });
  });

  it('does not open a self channel route when no channel is selected', async () => {
    vi.mocked(getSelectedChannelId).mockReturnValue(null);

    await expect(selfLoader()).rejects.toMatchObject({ status: 404 });
    expect(get).not.toHaveBeenCalled();
  });

  it('round-trips multiple socials and the short-link base through the update action', async () => {
    vi.mocked(put).mockResolvedValue({} as never);

    await formAction({
      request: formRequest({
        channelName: 'Updated',
        shortLinkUrl: 'https://morwalpiz.com/sl',
        socials: JSON.stringify([
          { provider: 'Instagram', handler: '@morwalpiz' },
          { provider: 'X', handler: 'morwalpiz' },
        ]),
      }),
      params: { id: 'channel-one' },
    } as never);

    expect(put).toHaveBeenCalledWith('/api/channels/channel-one', {
      channelId: 'channel-one',
      channelName: 'Updated',
      shortLinkUrl: 'https://morwalpiz.com/sl',
      socials: [
        { provider: 'instagram', handler: '@morwalpiz' },
        { provider: 'x', handler: 'morwalpiz' },
      ],
      socialPublishing: {
        telegram: { clearCredential: false },
        discord: { clearCredential: false },
        facebook: { clearCredential: false },
      },
    });
  });

  it('sends Analytics and editable YouTube ID on create and update', async () => {
    vi.mocked(post).mockResolvedValue({});
    vi.mocked(put).mockResolvedValue({});

    await formAction({
      request: formRequest({
        channelName: 'Channel',
        yTChannelId: 'UC_NEW',
        googleAnalyticsUrl: 'https://analytics.example/property',
      }),
      params: {},
    } as never);
    await formAction({
      request: formRequest({
        channelName: 'Channel',
        yTChannelId: 'UC_UPDATED',
        googleAnalyticsUrl: 'https://analytics.example/property',
      }),
      params: { id: 'stable-channel-key' },
    } as never);

    expect(post).toHaveBeenCalledWith(
      '/api/channels',
      expect.objectContaining({
        yTChannelId: 'UC_NEW',
        googleAnalyticsUrl: 'https://analytics.example/property',
      })
    );
    expect(put).toHaveBeenCalledWith(
      '/api/channels/stable-channel-key',
      expect.objectContaining({
        channelId: 'stable-channel-key',
        yTChannelId: 'UC_UPDATED',
        googleAnalyticsUrl: 'https://analytics.example/property',
      })
    );
  });

  it('preserves Analytics when the form omits it and maps backend field messages', async () => {
    vi.mocked(put).mockResolvedValue({
      errors: ['One or more validation errors occurred.'],
      fieldErrors: {
        GoogleAnalyticsUrl: ['Analytics URL is invalid.'],
        YTChannelId: ['Enter a YouTube channel ID.'],
        UnknownField: ['Review this value.'],
      },
      status: 400,
    } as never);

    const result = await formAction({
      request: formRequest({ channelName: 'Channel' }),
      params: { id: 'stable-channel-key' },
    } as never);

    expect(put).toHaveBeenCalledWith(
      '/api/channels/stable-channel-key',
      expect.not.objectContaining({ googleAnalyticsUrl: expect.anything() })
    );
    expect(result).toMatchObject({
      data: {
        success: false,
        errors: {
          googleAnalyticsUrl: 'Analytics URL is invalid.',
          yTChannelId: 'Enter a YouTube channel ID.',
          generics: ['UnknownField: Review this value.'],
        },
      },
    });
    expect(responseStatus(result)).toBe(400);
  });

  it('sends replace and clear intent without requiring existing credentials', async () => {
    vi.mocked(put).mockResolvedValue({} as never);

    await formAction({
      request: formRequest({
        channelName: 'Updated',
        socialPublishing: JSON.stringify({
          telegram: { destinationId: '-100123', credential: 'new-token', clearCredential: false },
          discord: { destinationId: '456', credential: '', clearCredential: true },
          facebook: { destinationId: '789', credential: '', clearCredential: false },
        }),
      }),
      params: { id: 'channel-one' },
    } as never);

    expect(put).toHaveBeenCalledWith(
      '/api/channels/channel-one',
      expect.objectContaining({
        socialPublishing: {
          telegram: { destinationId: '-100123', credential: 'new-token', clearCredential: false },
          discord: { destinationId: '456', credential: undefined, clearCredential: true },
          facebook: { destinationId: '789', credential: undefined, clearCredential: false },
        },
      })
    );
  });
});
