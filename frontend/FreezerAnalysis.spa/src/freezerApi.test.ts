import { afterEach, describe, expect, it, vi } from 'vitest';
import { freezerApi } from './freezerApi';

describe('freezerApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

  it('sends the CSRF token and session credentials for profile changes', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf-value' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        accountId: 'account-1',
        identityId: '12345',
        division: 'PROD',
        powerFactor: 'Major',
        performance: 0.94,
        updatedAtUtc: '2026-01-01T00:00:00Z'
      }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    await freezerApi.saveProfile({
      identityId: '12345',
      division: 'PROD',
      powerFactor: 'Major',
      performance: 0.94
    });

    const request = fetchMock.mock.calls[1]?.[1] as RequestInit;
    expect(new Headers(request.headers).get('X-CSRF-TOKEN')).toBe('csrf-value');
    expect(request.credentials).toBe('include');
    expect(request.method).toBe('PUT');
  });

  it('surfaces server problem details instead of masking them', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ title: 'Revisione in conflitto.' }), {
        status: 409,
        headers: { 'Content-Type': 'application/problem+json' }
      })
    ));

    await expect(freezerApi.getMatches()).rejects.toMatchObject({
      message: 'Revisione in conflitto.',
      status: 409
    });
  });

  it('sends paired PDFs to the authenticated preview-only endpoint', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, _init?: RequestInit) => Promise.resolve(
      new Response(
        JSON.stringify(String(input).includes('/api/v1/account/csrf')
          ? { token: 'csrf-value' }
          : { canPublish: false }),
        { status: 200 }
      )
    ));
    vi.stubGlobal('fetch', fetchMock);
    const form = new FormData();
    form.set('classificationFile', new File(['%PDF-classification'], 'classifica.pdf', { type: 'application/pdf' }));
    form.set('verificationFile', new File(['%PDF-verification'], 'verify.pdf', { type: 'application/pdf' }));

    await expect(freezerApi.previewPdfPair(form)).resolves.toMatchObject({ canPublish: false });

    const requestIndex = fetchMock.mock.calls.findIndex(([input]) =>
      String(input).endsWith('/api/v1/imports/pdfs/preview')
    );
    expect(requestIndex).toBeGreaterThanOrEqual(0);
    const request = fetchMock.mock.calls[requestIndex]?.[1] as RequestInit;
    expect(request.method).toBe('POST');
    expect(request.credentials).toBe('include');
    expect(new Headers(request.headers).get('X-CSRF-TOKEN')).toBe('csrf-value');
    expect(request.body).toBe(form);
  });

  it('submits verification against the explicit final revision', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, _init?: RequestInit) => Promise.resolve(
      new Response(
        JSON.stringify(String(input).includes('/api/v1/account/csrf')
          ? { token: 'csrf-value' }
          : { id: 'report-1', snapshotId: 'snapshot-1', finalRevisionId: 'revision-final' }),
        { status: 200 }
      )
    ));
    vi.stubGlobal('fetch', fetchMock);

    await expect(freezerApi.verifySnapshot('snapshot-1', 'revision-final')).resolves.toMatchObject({
      finalRevisionId: 'revision-final'
    });

    const requestIndex = fetchMock.mock.calls.findIndex(([input]) =>
      String(input).endsWith('/api/v1/snapshots/snapshot-1/verification')
    );
    expect(requestIndex).toBeGreaterThanOrEqual(0);
    const [url, request] = fetchMock.mock.calls[requestIndex]!;
    expect(String(url)).toBe('/api/v1/snapshots/snapshot-1/verification');
    expect(request?.method).toBe('POST');
    expect(JSON.parse(String(request?.body))).toEqual({ finalRevisionId: 'revision-final' });
  });

  it('requests replay from the authenticated frozen snapshot endpoint', async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, _init?: RequestInit) => Promise.resolve(
      new Response(
        JSON.stringify(String(input).includes('/api/v1/account/csrf')
          ? { token: 'csrf-value' }
          : {
            snapshotId: 'snapshot-1',
            engineVersion: 'engine-v1',
            isReplayAvailable: true,
            matchesFrozenForecast: true,
            stages: [],
            diagnostics: []
          }),
        { status: 200 }
      )
    ));
    vi.stubGlobal('fetch', fetchMock);

    await expect(freezerApi.replaySnapshot('snapshot-1')).resolves.toMatchObject({
      snapshotId: 'snapshot-1',
      isReplayAvailable: true
    });

    const [url, request] = fetchMock.mock.calls.find(([input]) =>
      String(input).endsWith('/api/v1/snapshots/snapshot-1/replay')
    )!;
    expect(String(url)).toBe('/api/v1/snapshots/snapshot-1/replay');
    expect(request?.method).toBe('GET');
    expect(request?.credentials).toBe('include');
  });
});
