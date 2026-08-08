import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  Delete,
  get,
  put,
  post,
  resetCsrfToken,
  setAuthTokenProvider,
  setCookieOnlyMode,
  setSelectedChannelId,
  setRequestCredentialsMode,
} from '@morwalpizvideo/services';

describe('shared API client CSRF integration', () => {
  beforeEach(() => {
    resetCsrfToken();
    setAuthTokenProvider(() => null);
    setCookieOnlyMode(false);
    setSelectedChannelId(null);
    setRequestCredentialsMode('include');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('acquires and sends a CSRF token for unsafe cookie requests', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf-token' }))
      .mockResolvedValueOnce(jsonResponse({ saved: true }));
    vi.stubGlobal('fetch', fetchMock);

    await post('/api/protected-resource', { value: 1 });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/auth/csrf', {
      method: 'GET',
      credentials: 'include',
    });
    const request = fetchMock.mock.calls[1][1] as RequestInit;
    expect(request.credentials).toBe('include');
    expect((request.headers as Headers).get('X-CSRF-TOKEN')).toBe('csrf-token');
  });

  it('reuses the token until session state resets it', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'first-token' }))
      .mockResolvedValueOnce(jsonResponse({ saved: true }))
      .mockResolvedValueOnce(jsonResponse({ saved: true }))
      .mockResolvedValueOnce(jsonResponse({ token: 'second-token' }))
      .mockResolvedValueOnce(jsonResponse({ saved: true }));
    vi.stubGlobal('fetch', fetchMock);

    await post('/api/first', {});
    await post('/api/second', {});
    resetCsrfToken();
    await post('/api/third', {});

    expect(fetchMock).toHaveBeenCalledTimes(5);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/auth/csrf');
    expect(fetchMock.mock.calls[3][0]).toBe('/api/auth/csrf');
    const thirdRequest = fetchMock.mock.calls[4][1] as RequestInit;
    expect((thirdRequest.headers as Headers).get('X-CSRF-TOKEN')).toBe('second-token');
  });

  it('does not require a CSRF token to log in', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse({ user: { username: 'admin' } }));
    vi.stubGlobal('fetch', fetchMock);

    await post('/api/auth/login', { username: 'admin', password: 'secret' });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const request = fetchMock.mock.calls[0][1] as RequestInit;
    expect((request.headers as Headers).has('X-CSRF-TOKEN')).toBe(false);
  });

  it('rethrows network failures from delete requests', async () => {
    const networkError = new TypeError('Failed to fetch');
    setRequestCredentialsMode('omit');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(networkError));

    await expect(Delete('/api/channels/channel-one')).rejects.toBe(networkError);
  });

  it('sends the selected channel header for scoped routes and skips channel collection endpoints', async () => {
    setSelectedChannelId('channel-one');
    setRequestCredentialsMode('omit');
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(jsonResponse({ data: [] })));
    vi.stubGlobal('fetch', fetchMock);

    await get('api/videos');
    await get('/api/channels/channel-one');
    await put('/api/channels/channel-one', { channelName: 'Updated channel' });
    await Delete('/api/channels/channel-one');
    await get('/api/channels');
    await get('/api/channels/accessible');

    expect(fetchMock.mock.calls.slice(0, 4).map(([url]) => url)).toEqual([
      '/api/videos',
      '/api/channels/channel-one',
      '/api/channels/channel-one',
      '/api/channels/channel-one',
    ]);
    expect(fetchMock.mock.calls.slice(0, 4).map(([, request]) => (request as RequestInit).method)).toEqual([
      'GET',
      'GET',
      'PUT',
      'DELETE',
    ]);

    for (const [, request] of fetchMock.mock.calls.slice(0, 4)) {
      expect(new Headers((request as RequestInit).headers).get('X-Channel-Id')).toBe('channel-one');
    }

    const collectionHeaders = new Headers((fetchMock.mock.calls[4][1] as RequestInit).headers);
    expect(collectionHeaders.has('X-Channel-Id')).toBe(false);

    const accessibleCollectionHeaders = new Headers((fetchMock.mock.calls[5][1] as RequestInit).headers);
    expect(accessibleCollectionHeaders.has('X-Channel-Id')).toBe(false);
  });

  it('uses the HttpOnly cookie instead of localStorage or a browser bearer header', async () => {
    localStorage.setItem('authToken', 'browser-token');
    setCookieOnlyMode(true);

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf-token' }))
      .mockResolvedValueOnce(jsonResponse({ saved: true }));
    vi.stubGlobal('fetch', fetchMock);

    await post('/api/protected-resource', { value: 1 });

    const request = fetchMock.mock.calls[1][1] as RequestInit;
    const headers = request.headers as Headers;
    expect(headers.has('Authorization')).toBe(false);
    expect(request.credentials).toBe('include');
  });
});

function jsonResponse(payload: object): Response {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}