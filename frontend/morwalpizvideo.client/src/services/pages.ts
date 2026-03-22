import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

export function getPages(id: string) {
    return get(ComposeUrl(frontendEndpoints.PAGES_DETAIL, { pageId: id }));
}