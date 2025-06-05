export function getMatchLinktree(matchId) {
    return fetch(`/api/YouTubeVideoLinks/${matchId}/links`)
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
}

export function getMatch(matchId) {
    return fetch(`/api/matches/${matchId}`)
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
}

export function getCreatorImage(imageName) {
    return `/api/YouTubeVideoLinks/image/${imageName}`;
}
