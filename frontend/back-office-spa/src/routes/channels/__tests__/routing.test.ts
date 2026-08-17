import { beforeEach, describe, expect, it, vi } from 'vitest';
import { get, put, Delete, getSelectedChannelId } from '@morwalpizvideo/services';
import detailLoader from '../detail/loader';
import formLoader from '../form/loader';
import formAction from '../form/action';
import indexLoader from '../index/loader';
import indexAction from '../index/action';
import selfLoader from '../self/loader';

vi.mock('@morwalpizvideo/services', () => ({
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
  it('throws a router 404 instead of returning an API error as a Channel', async () => {
    vi.mocked(get).mockResolvedValue(scopedNotFound as never);

    await expect(detailLoader({ params: { id: 'missing-channel' } } as never))
      .rejects.toMatchObject({ status: 404 });
    await expect(formLoader({ params: { id: 'missing-channel' } }))
      .rejects.toMatchObject({ status: 404 });
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

    expect(put).toHaveBeenCalledWith(
      '/api/channels/channel-one',
      {
        channelId: 'channel-one',
        channelName: 'Updated',
        shortLinkUrl: 'https://morwalpiz.com/sl',
        socials: [
          { provider: 'instagram', handler: '@morwalpiz' },
          { provider: 'x', handler: 'morwalpiz' },
        ],
      }
    );
  });
});
