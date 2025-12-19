

/**
 * Interface representing a video in the system
 */
export interface Video {
  id: string;
  matchId: string;
  title: string;
  description?: string;
  thumbnailVideoId: string;
  isLink: boolean; // For backward compatibility
  matchType?: number; // MatchType enum value
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
  const videos = await fetch(`/api/videos`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading videos:', error);
      return [];
    });
  
  return {
    videos,
  };
}
