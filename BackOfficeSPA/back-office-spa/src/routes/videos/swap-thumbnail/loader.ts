import { API_CONFIG } from '@config/api';

/**
 * Interface representing a video in the system
 */
export interface Video {
  id: string;
  title: string;
  description?: string;
}

/**
 * Interface for the loader response containing videos and categories
 */
export interface LoaderData {
  videos: Video[];
}

/**
 * Loader function that fetches both videos and categories from the API
 * @returns Promise with videos and categories
 */
export default async function loader(): Promise<LoaderData> {
  // Fetch videos
  const videos = await fetch(`${API_CONFIG.BASE_URL}/videos`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading videos:', error);
      return [];
    });
  
  return {
    videos,
  };
}
