import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

export function getMatches() {
    return get(frontendEndpoints.MATCHES);
}

export function getMatch(id: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_DETAIL, { matchId: id }));
}

export function getMatchImages(id: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_IMAGES, { matchId: id }));
}