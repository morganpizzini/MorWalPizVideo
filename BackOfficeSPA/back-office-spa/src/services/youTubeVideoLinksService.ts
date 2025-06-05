import { API_CONFIG } from '@config/api';
import { CreateYouTubeVideoLinkRequest, YouTubeVideoLinkResponse } from '@models/youTubeVideoLink';

/**
 * Service for managing YouTube video links associated with matches
 */
export class YouTubeVideoLinksService {
  /**
   * Get all YouTube video links for a specific match
   */
  static async getVideoLinks(matchId: string): Promise<YouTubeVideoLinkResponse[]> {
    const response = await fetch(`${API_CONFIG.BASE_URL}/YouTubeVideoLinks/${matchId}/links`);
    
    if (!response.ok) {
      throw new Error(`Failed to fetch video links: ${response.statusText}`);
    }
    
    return response.json();
  }

  /**
   * Create a new YouTube video link for a match
   */
  static async createVideoLink(request: CreateYouTubeVideoLinkRequest): Promise<YouTubeVideoLinkResponse> {
    const response = await fetch(`${API_CONFIG.BASE_URL}/YouTubeVideoLinks/create`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to create video link: ${errorText}`);
    }

    return response.json();
  }

  /**
   * Remove a YouTube video link from a match
   */
  static async removeVideoLink(matchId: string, youtubeVideoId: string): Promise<void> {
    const response = await fetch(`${API_CONFIG.BASE_URL}/YouTubeVideoLinks/${matchId}/links/${youtubeVideoId}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to remove video link: ${errorText}`);
    }
  }

  /**
   * Get the URL for a creator image
   */
  static getCreatorImageUrl(imageName: string): string {
    return `${API_CONFIG.BASE_URL}/YouTubeVideoLinks/image/${imageName}`;
  }
}
