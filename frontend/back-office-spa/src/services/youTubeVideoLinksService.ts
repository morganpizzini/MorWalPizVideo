
import { CreateYouTubeVideoLinkRequest, YouTubeVideoLinkResponse } from '@morwalpizvideo/models';
import { get, post, Delete } from '@morwalpizvideo/services';

/**
 * Service for managing YouTube video links associated with matches
 */
export class YouTubeVideoLinksService {
  /**
   * Get all YouTube video links for a specific match
   */
  static async getVideoLinks(matchId: string): Promise<YouTubeVideoLinkResponse[]> {
    return get(`/api/YouTubeVideoLinks/${matchId}/links`);
  }

  /**
   * Create a new YouTube video link for a match
   */
  static async createVideoLink(request: CreateYouTubeVideoLinkRequest): Promise<YouTubeVideoLinkResponse> {
    return post(`/api/YouTubeVideoLinks/create`, request);
  }

  /**
   * Remove a YouTube video link from a match
   */
  static async removeVideoLink(matchId: string, youtubeVideoId: string): Promise<void> {
    await Delete(`/api/YouTubeVideoLinks/${matchId}/links/${youtubeVideoId}`);
  }

  /**
   * Get the URL for a creator image
   */
  static getCreatorImageUrl(imageName: string): string {
    return `/api/YouTubeVideoLinks/image/${imageName}`;
  }
}
