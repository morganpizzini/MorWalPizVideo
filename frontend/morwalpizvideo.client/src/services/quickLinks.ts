import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { QuickLinks } from '@morwalpizvideo/models';

export function getQuickLinks(url: string): Promise<QuickLinks> {
  const normalizedUrl = url.trim().replace(/^\/+|\/+$/g, '');
  return get(ComposeUrl(frontendEndpoints.QUICK_LINKS_DETAIL, { url: encodeURIComponent(normalizedUrl) }));
}
