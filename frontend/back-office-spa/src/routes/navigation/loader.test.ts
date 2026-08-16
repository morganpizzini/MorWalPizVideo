import { describe, expect, it, vi } from 'vitest';
import { fetchPages, getNavigation } from '@morwalpizvideo/services';
import loader from './loader';

vi.mock('@morwalpizvideo/services', () => ({
  fetchPages: vi.fn(),
  getNavigation: vi.fn(),
}));

describe('navigation loader', () => {
  it('loads navigation and pages together for the selected channel', async () => {
    const navigation = { channelId: 'channel-1', isActive: true, headerItems: [], footerColumnCount: 1, footerItems: [] };
    const pages = [{ id: 'page-1', title: 'About', status: 1 }];
    vi.mocked(getNavigation).mockResolvedValue(navigation as never);
    vi.mocked(fetchPages).mockResolvedValue(pages as never);

    await expect(loader()).resolves.toEqual({ navigation, pages });
  });
});