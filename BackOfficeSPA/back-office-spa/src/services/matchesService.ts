import { API_CONFIG } from '@config/api';

/**
 * Enum representing the type of match in the system
 */
export enum MatchType {
  SingleVideo = 0,
  Collection = 1
}

/**
 * Interface representing a lightweight reference to a video
 */
export interface VideoRef {
  youtubeId: string;
  category: string;
}

/**
 * Represents a video in the system
 */
export interface Video {
  youtubeId: string;
  title: string;
  description: string;
  thumbnail?: string;
  duration?: string;
  views?: number;
  likes?: number;
  comments?: number;
  publishedAt?: string;
  category: string;
}

/**
 * Represents a match in the system with video information
 */
export interface Match {
  id: string;
  matchId: string;
  title: string;
  description: string;
  url: string;
  thumbnailVideoId: string;
  videoRefs: VideoRef[];
  category: string;
  matchType: MatchType;
  // Backward compatibility
  thumbnailUrl?: string;
  isLink?: boolean;
  videos?: Video[];
}

/**
 * Fetches all available matches from the API
 * @returns Promise with the list of matches
 */
export const fetchMatches = async (): Promise<Match[]> => {
  return fetch(`/api/videos`)
    .then(response => {
      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }
      return response.json();
    })
    .catch(error => {
      console.error('Error fetching matches:', error);
      return [];
    });
};
