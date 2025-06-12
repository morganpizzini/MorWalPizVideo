

/**
 * Interface representing a match in the system
 */
export interface Match {
  id: string;
  title: string;
  url: string;
}

/**
 * Interface for the loader response containing matches
 */
export interface LoaderData {
  matches: Match[];
}

/**
 * Loader function that fetches matches from the API
 * @returns Promise with matches
 */
export default async function loader(): Promise<LoaderData> {
  // Fetch matches
  const matchesPromise = fetch(`/api/matches`)
    .then(response => response.json())
    .catch(error => {
      console.error('Error loading matches:', error);
      return [];
    });

  const matches = await matchesPromise;

  return {
    matches,
  };
}