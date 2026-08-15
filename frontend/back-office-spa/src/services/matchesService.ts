import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { Match } from '@morwalpizvideo/models';

export type { Match } from '@morwalpizvideo/models';

/**
 * Fetches all available matches from the API
 * @returns Promise with the list of matches
 */
export const fetchMatches = async (): Promise<Match[]> => {
  try {
    return await get(endpoints.VIDEOS);
  } catch (error) {
    console.error('Error fetching matches:', error);
    return [];
  }
};

export const getMatch = async (id: string): Promise<Match> => {
    const response = await get(ComposeUrl(endpoints.VIDEOS_DETAIL, { videoId: id }));
    return {
        ...response,
        breadcrumbIdentifier: response.title
    };
};