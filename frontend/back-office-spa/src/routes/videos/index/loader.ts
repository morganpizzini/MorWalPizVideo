import { fetchMatches } from '../../../services/matchesService';
import { get, endpoints } from '@morwalpizvideo/services';
import type { Channel } from '@morwalpizvideo/models';

/**
 * Loader function for the videos index route
 * Fetches all matches/videos from the API
 */
export default async function(){
  try {
    const [matches, channelsResult] = await Promise.all([
      fetchMatches(),
      get(endpoints.CHANNELS_ACCESSIBLE).then(value => ({ value: value as Channel[], error: undefined })).catch(error => ({ value: [], error })),
    ]);
    if (channelsResult.error) {
      console.warn('Accessible channel metadata unavailable while loading videos.', channelsResult.error);
    }
    return { matches, channels: channelsResult.value };
  } catch (error) {
    console.error('Error loading videos:', error);
    throw new Response('Failed to load videos', { status: 500 });
  }
};
