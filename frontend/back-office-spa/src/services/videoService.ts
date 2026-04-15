// Service for interacting with the Video API endpoints

import { post } from '@morwalpizvideo/services';
import {
  VideoImportRequest,
  SwapRootThumbnailRequest,
  RootCreationRequest,
  SubVideoCrationRequest,
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

  // Convert a video to root
  convertToRoot: async (request: RootCreationRequest): Promise<void> => {
    await post(`/api/Video/ConvertIntoRoot`, request);
  },

  // Swap thumbnail URL
  swapThumbnailUrl: async (request: SwapRootThumbnailRequest): Promise<void> => {
    await post(`/api/Video/SwapThumbnailId`, request);
  },

  // Create a root video
  createRoot: async (request: RootCreationRequest): Promise<void> => {
    await post(`/api/Video/RootCreation`, request);
  },

  // Create a sub-video
  createSubVideo: async (request: SubVideoCrationRequest): Promise<void> => {
    await post(`/api/Video/ImportSubCreation`, request);
  },

  // Get review details
  getReviewDetails: async (reviewText: string): Promise<ReviewDetails> => {
    return post(`/api/Chat`, reviewText);
  },

  // Publish video to social media
  publishToSocial: async (videoId: string, message: string): Promise<void> => {
    await post(`/api/Videos/${videoId}/publish-social`, { message });
  },
};

// Export individual function for convenience
export const publishVideoToSocial = VideoService.publishToSocial;
