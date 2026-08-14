import { ComposeUrl, frontendEndpoints, get } from '@morwalpizvideo/services';
import type { QuickLinks } from '@morwalpizvideo/models';

export function getShootingItaQuickLinks(customLinktree: string): Promise<QuickLinks> {
    const normalizedSlug = customLinktree.trim().replace(/^\/+|\/+$/g, '');
    return get(ComposeUrl(frontendEndpoints.SHIT_QUICK_LINK_DETAIL, {
        customLinktree: encodeURIComponent(normalizedSlug),
    })) as Promise<QuickLinks>;
}