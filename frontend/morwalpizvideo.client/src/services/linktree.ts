import { get, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

export function getMatchLinktree(matchId: string) {
    return get(ComposeUrl(frontendEndpoints.YOUTUBE_VIDEO_LINKS_DETAIL, { matchId }));
}

export function getMatch(matchId: string) {
    return get(ComposeUrl(frontendEndpoints.MATCHES_DETAIL, { matchId }));
}

export function getCreatorImage(imageName: string) {
    return ComposeUrl(frontendEndpoints.YOUTUBE_VIDEO_LINKS_IMAGE, { imageName });
}