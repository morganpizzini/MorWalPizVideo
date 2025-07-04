

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
 * Interface representing a category in the system
 */
export interface Category {
  categoryId: string;
  title: string;
}

/**
 * Interface for the loader response containing videos and categories
 */
export interface LoaderData {
  videos: Video[];
  categories: Category[];
}

/**
 * Loader function that fetches both videos and categories from the API
 * @returns Promise with videos and categories
 */
export default async function loader(): Promise<LoaderData> {
  // Fetch videos
  const videosPromise = fetch(`/api/videos`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading videos:', error);
      return [];
    });

  // Fetch categories
  const categoriesPromise = fetch(`/api/categories`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading categories:', error);
      return [];
    });

  // Wait for both requests to complete
  const [videos, categories] = await Promise.all([videosPromise, categoriesPromise]);

  return {
    videos,
    categories,
  };
}
