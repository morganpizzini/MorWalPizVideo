// Service for interacting with the Video API endpoints

import { post } from '@morwalpizvideo/services';
import {
  VideoImportRequest,
  ReviewDetails,
} from '@morwalpizvideo/models';

export const VideoService = {
  // Translate video shorts
  translateShort: async (videoIds: string[]): Promise<void> => {
    await post(`/api/Video/Translate`, videoIds);
  },

  // Import a video
  importVideo: async (request: VideoImportRequest): Promise<void> => {
    await post(`/api/Video/ImportVideo`, request);
  },

  // Get review details
  getReviewDetails: async (reviewText: string): Promise<ReviewDetails> => {
    return post(`/api/Chat`, reviewText);
  },

  // Publish video to social media
  publishToSocial: async (videoId: string, message: string): Promise<void> => {
    await post(`/api/Videos/${videoId}/publish-social`, { message });
  },

  // Refresh YouTube metadata for a video
  refreshYouTubeData: async (videoId: string): Promise<void> => {
    await post(`/api/Videos/${videoId}/refresh-youtube`, {});
  },
};

// Export individual function for convenience
export const publishVideoToSocial = VideoService.publishToSocial;
export const refreshVideoYouTubeData = VideoService.refreshYouTubeData;
