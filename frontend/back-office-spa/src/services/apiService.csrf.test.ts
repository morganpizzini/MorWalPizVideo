import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  post,
  resetCsrfToken,
  setAuthTokenProvider,
  setRequestCredentialsMode,
} from '@morwalpizvideo/services';

describe('shared API client CSRF integration', () => {
  beforeEach(() => {
    resetCsrfToken();
    setAuthTokenProvider(() => null);
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
});

function jsonResponse(payload: object): Response {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}