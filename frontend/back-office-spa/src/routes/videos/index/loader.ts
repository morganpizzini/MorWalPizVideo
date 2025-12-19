import { fetchMatches } from '../../../services/matchesService';

/**
 * Loader function for the videos index route
 * Fetches all matches/videos from the API
 */
export default async function(){
  try {
    const matches = await fetchMatches();
    return { matches };
  } catch (error) {
    console.error('Error loading videos:', error);
    throw new Response('Failed to load videos', { status: 500 });
  }
};
