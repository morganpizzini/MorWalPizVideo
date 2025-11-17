import { LoaderFunctionArgs } from 'react-router';
import { fetchMatches } from '../../../services/matchesService';

/**
 * Loader function for the video detail route
 * Fetches a specific match/video by ID
 */
export default async function({ params }: LoaderFunctionArgs) {
  const { id } = params;
  
  if (!id) {
    throw new Response('Video ID is required', { status: 400 });
  }

  try {
    const matches = await fetchMatches();
    const match = matches.find(m => m.id === id);
    
    if (!match) {
      throw new Response('Video not found', { status: 404 });
    }
    
    return { match };
  } catch (error) {
    console.error('Error loading video details:', error);
    throw new Response('Failed to load video details', { status: 500 });
  }
};
