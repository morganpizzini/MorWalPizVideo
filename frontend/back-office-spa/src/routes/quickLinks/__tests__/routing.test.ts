import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createQuickLinks, deleteQuickLinks, fetchQuickLinks, getQuickLinks, updateQuickLinks } from '@morwalpizvideo/services';
import formAction from '../form/action';
import formLoader from '../form/loader';
import indexAction from '../index/action';
import indexLoader from '../index/loader';

vi.mock('@morwalpizvideo/services', () => ({
  createQuickLinks: vi.fn(),
  deleteQuickLinks: vi.fn(),
  fetchQuickLinks: vi.fn(),
  getQuickLinks: vi.fn(),
  updateQuickLinks: vi.fn(),
}));

const apiError = {
  errors: ['A QuickLinks page with this url already exists.'],
  status: 409,
};

function responseStatus(result: unknown): number | undefined {
  if (result instanceof Response) return result.status;
  if (typeof result === 'object' && result !== null && 'init' in result) {
    return (result as { init?: ResponseInit }).init?.status;
  }
  return undefined;
}

function formRequest(values: Record<string, string>) {
  return new Request('http://localhost/quicklinks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams(values).toString(),
  });
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('QuickLinks route error handling', () => {
  it('returns a conflict response instead of reporting a successful save', async () => {
    vi.mocked(createQuickLinks).mockResolvedValue(apiError as never);

    const result = await formAction({
      request: formRequest({ title: 'Links', url: 'existing', links: '[]' }),
      params: {},
    } as never);

    expect(responseStatus(result)).toBe(409);
    expect((result as { data?: { success?: boolean; errors?: { generics?: string[] } } }).data?.success).toBe(false);
    expect((result as { data?: { errors?: { generics?: string[] } } }).data?.errors?.generics).toContain(apiError.errors[0]);
  });

  it('returns API errors from delete instead of treating an envelope as success', async () => {
    vi.mocked(deleteQuickLinks).mockResolvedValue({ errors: ['Delete failed'], status: 400 } as never);

    const result = await indexAction({ request: formRequest({ id: 'quick-links-1' }) });

    expect(responseStatus(result)).toBe(400);
    expect((result as { data?: { errors?: string[] } }).data?.errors).toEqual(['Delete failed']);
  });

  it('throws loader errors for detail and index API envelopes', async () => {
    vi.mocked(getQuickLinks).mockResolvedValue({ errors: ['Not found'], status: 404 } as never);
    vi.mocked(fetchQuickLinks).mockResolvedValue({ errors: ['Unable to load'], status: 500 } as never);

    await expect(formLoader({ params: { id: 'missing' } } as never)).rejects.toMatchObject({ status: 404 });
    await expect(indexLoader()).rejects.toMatchObject({ status: 500 });
  });

  it('keeps successful create and update responses successful', async () => {
    vi.mocked(createQuickLinks).mockResolvedValue({} as never);
    vi.mocked(updateQuickLinks).mockResolvedValue({} as never);

    const createResult = await formAction({
      request: formRequest({ title: 'Links', url: 'new-links', links: '[]' }),
      params: {},
    } as never);
    const updateResult = await formAction({
      request: formRequest({ title: 'Links', url: 'updated-links', links: '[]' }),
      params: { id: 'quick-links-1' },
    } as never);

    expect(responseStatus(createResult)).toBe(201);
    expect(responseStatus(updateResult)).toBe(200);
  });
});