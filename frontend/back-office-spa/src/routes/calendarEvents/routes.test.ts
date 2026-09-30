import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiResponseError,
  get,
  post,
  put,
  Delete,
} from '../../../../fe-packages/services/dist/apiService';
import editLoader from './edit/loader';
import detailLoader from './detail/loader';
import editAction from './edit/action';
import createAction from './create/action';
import deleteAction from './index/action';
import detailDeleteAction from './detail/action';

vi.mock('@morwalpizvideo/services', async () => ({
  ...(await vi.importActual<typeof import('@morwalpizvideo/services')>('@morwalpizvideo/services')),
  ...(await import('../../../../fe-packages/services/dist/calendarService')),
  ...(await import('../../../../fe-packages/services/dist/apiService')),
}));
vi.mock('../../../../fe-packages/services/dist/apiService', async () => ({
  ...(await vi.importActual<typeof import('../../../../fe-packages/services/dist/apiService')>(
    '../../../../fe-packages/services/dist/apiService'
  )),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  Delete: vi.fn(),
}));

const event = { id: 'persisted-id', title: 'New title', revision: 2, categories: [] };
function request(values: Record<string, string>) {
  return new Request('https://example.test/calendarevents/Old%20title/edit', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams(values).toString(),
  });
}
const values = {
  id: event.id,
  title: 'Old title',
  newTitle: event.title,
  description: 'Description',
  startDate: '2026-01-01',
  endDate: '2026-01-02',
  matchId: 'match-id',
  revision: '1',
  categories: JSON.stringify([{ id: 'category-id', title: 'Category title' }]),
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(put).mockResolvedValue(event);
  vi.mocked(post).mockResolvedValue(event);
  vi.mocked(Delete).mockResolvedValue(undefined);
});

describe('Calendar routes and shared transport', () => {
  it('uses the id route parameter as an already decoded title and loads content categories', async () => {
    vi.mocked(get).mockImplementation(async path =>
      path === 'api/categories' ? [{ id: 'category-id', title: 'Category title' }] : event
    );
    const result = await editLoader({ params: { id: '100% event / title' } } as never);
    expect(get).toHaveBeenCalledWith('api/calendarEvents/by-title/100%25%20event%20%2F%20title');
    expect(get).toHaveBeenCalledWith('api/categories');
    expect(result).toEqual({
      calendarEvent: event,
      categories: [{ id: 'category-id', title: 'Category title' }],
    });
    await expect(detailLoader({ params: { id: 'New title' } } as never)).resolves.toEqual(event);
  });

  it('updates by persisted ID with renamed title, category references and numeric revision', async () => {
    const result = await editAction({ request: request(values) });
    expect(put).toHaveBeenCalledWith('api/calendarEvents/persisted-id', {
      id: event.id,
      title: event.title,
      description: values.description,
      startDate: values.startDate,
      endDate: values.endDate,
      matchId: values.matchId,
      revision: 1,
      categories: [{ id: 'category-id', title: 'Category title' }],
    });
    expect(result.data).toEqual({ success: true, updatedTitle: event.title });
  });

  it('creates using the input title and exact embedded categories', async () => {
    const result = await createAction({ request: request(values) });
    expect(post).toHaveBeenCalledWith(
      'api/calendarEvents',
      expect.objectContaining({
        title: 'Old title',
        categories: [{ id: 'category-id', title: 'Category title' }],
      })
    );
    expect(result.init?.status).toBe(201);
  });

  it.each([deleteAction, detailDeleteAction])(
    'deletes by persisted ID and expected revision',
    async action => {
      await action({ request: request(values) } as never);
      expect(Delete).toHaveBeenCalledWith('api/calendarEvents/persisted-id?revision=1');
    }
  );

  it('preserves 409 and exposes conflict recovery rather than reporting success', async () => {
    vi.mocked(put).mockResolvedValue({
      status: 409,
      errors: ['Calendar event changed. Reload before saving.'],
    });
    const result = await editAction({ request: request(values) });
    expect(result.init?.status).toBe(409);
    expect(result.data).toMatchObject({ success: false, conflict: true });
    expect(result.data.errors?.generics?.[0]).toContain('Reload');
  });

  it('maps authoritative server field validation to form fields', async () => {
    vi.mocked(post).mockResolvedValue({
      status: 400,
      fieldErrors: { Title: ['Title is required'] },
      errors: ['Invalid'],
    });
    const result = await createAction({ request: request(values) });
    expect(result.init?.status).toBe(400);
    expect(result.data.errors?.fields?.title).toEqual(['Title is required']);
  });

  it('does not silently submit an omitted revision or malformed categories', async () => {
    const missing = await editAction({ request: request({ ...values, revision: '' }) });
    const malformed = await createAction({
      request: request({ ...values, categories: '{invalid' }),
    });
    expect(missing.init?.status).toBe(400);
    expect(malformed.init?.status).toBe(400);
    expect(put).not.toHaveBeenCalled();
    expect(post).not.toHaveBeenCalled();
  });

  it('does not mask permission or server failures as not found', async () => {
    vi.mocked(get).mockResolvedValue({ status: 403, errors: ['Forbidden'] });
    await expect(detailLoader({ params: { id: 'title' } } as never)).rejects.toBeInstanceOf(
      ApiResponseError
    );
  });
});
