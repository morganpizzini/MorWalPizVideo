import { LoaderFunctionArgs } from 'react-router';
import { getMatch } from '../../../services/matchesService';

/**
 * Interface representing a category in the system
 */
export interface Category {
  categoryId: string;
  title: string;
}

/**
 * Loader function for the video detail route
 * Fetches a specific match/video by ID and available categories
 */
export default async function({ params }: LoaderFunctionArgs) {
  const { id } = params;
  
  if (!id) {
    throw new Response('Video ID is required', { status: 400 });
  }

  try {
    // Fetch both the match and categories in parallel
    const [match, categories] = await Promise.all([
      getMatch(id),
      fetch('/api/categories')
        .then(response => response.json())
        .catch(error => {
          console.error('Error loading categories:', error);
          return [];
        })
    ]);
    
    if (!match) {
      throw new Response('Video not found', { status: 404 });
    }
    
    return { match, categories };
  } catch (error) {
    console.error('Error loading video details:', error);
    throw new Response('Failed to load video details', { status: 500 });
  }
};
