import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

export function getMatchLinktree(matchId: string) {
    return get(ComposeUrl(frontendEndpoints.LINKTREE_DETAIL, { matchId }));
}

export function getMatch(matchId: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_DETAIL, { matchId }));
}

export function getCreatorImage(imageName: string) {
    return ComposeUrl(frontendEndpoints.LINKTREE_IMAGE, { imageName });
}