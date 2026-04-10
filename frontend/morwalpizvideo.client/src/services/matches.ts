import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

export function getMatches(returnFullRresponse: boolean = false) {
    return get(frontendEndpoints.MATCHES, null, null, returnFullRresponse);
}

export function getMatch(id: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_DETAIL, { matchId: id }));
}

export function getMatchImages(id: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_IMAGES, { matchId: id }));
}