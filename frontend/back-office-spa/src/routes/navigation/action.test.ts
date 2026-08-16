import { describe, expect, it, vi } from 'vitest';
import { NavigationItemType } from '@morwalpizvideo/models';
import { saveNavigation } from '@morwalpizvideo/services';
import action, { validateNavigation } from './action';

vi.mock('@morwalpizvideo/services', () => ({ saveNavigation: vi.fn() }));

describe('navigation action', () => {
  it('validates page, internal, external, and footer-column links', () => {
    const errors = validateNavigation({
      footerColumnCount: 2,
      headerItems: [
        { type: NavigationItemType.Page, pageId: '', targetUrl: '', displayText: 'Page', column: 0, displayOrder: 0 },
        { type: NavigationItemType.Internal, targetUrl: '//unsafe', displayText: 'Internal', column: 0, displayOrder: 1 },
        { type: NavigationItemType.External, targetUrl: 'javascript:alert(1)', displayText: 'External', column: 0, displayOrder: 2 },
      ],
      footerItems: [{ type: NavigationItemType.Internal, targetUrl: '/footer', displayText: 'Footer', column: 2, displayOrder: 0 }],
      isActive: true,
    });

    expect(errors).toEqual(expect.arrayContaining([
      'Menu page links must reference a page.',
      'Internal menu links must start with a single slash.',
      'External menu links must use http or https without credentials.',
      'Footer item column is outside the configured column count.',
    ]));
  });

  it('preserves the submitted menu order in the API payload', async () => {
    vi.mocked(saveNavigation).mockResolvedValue({} as never);
    const navigation = { footerColumnCount: 1, isActive: true, headerItems: [
      { type: NavigationItemType.Internal, targetUrl: '/first', displayText: 'First', column: 0, displayOrder: 0 },
      { type: NavigationItemType.External, targetUrl: 'https://example.test', displayText: 'Second', column: 0, displayOrder: 1 },
    ], footerItems: [] };
    const formData = new FormData();
    formData.set('navigation', JSON.stringify(navigation));

    await action({ request: new Request('http://localhost/navigation', { method: 'PUT', body: formData }) });

    expect(saveNavigation).toHaveBeenCalledWith(navigation);
  });
});