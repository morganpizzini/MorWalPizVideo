import { describe, expect, it, vi } from 'vitest';
import { fetchPages } from '@morwalpizvideo/services';
import loader from './loader';

vi.mock('@morwalpizvideo/services', () => ({ fetchPages: vi.fn() }));

describe('pages index loader', () => {
  it('returns the selected channel page collection from the API service', async () => {
    const pages = [{ id: 'page-1', title: 'About', status: 1 }];
    vi.mocked(fetchPages).mockResolvedValue(pages as never);

    await expect(loader()).resolves.toEqual(pages);
    expect(fetchPages).toHaveBeenCalledOnce();
  });
});