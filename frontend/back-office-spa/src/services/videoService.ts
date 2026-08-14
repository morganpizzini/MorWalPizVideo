// Service for interacting with the Video API endpoints

import { endpoints, get, post } from '@morwalpizvideo/services';
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
    await post(endpoints.VIDEOS_IMPORT, request);
  },

  getImportCandidates: async (startDate: string, endDate: string): Promise<ImportCandidate[]> =>
    get(`/api/Videos/import-candidates?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),

  bulkImport: async (request: BulkImportRequest): Promise<BulkImportResult[]> =>
    post('/api/Videos/bulk-import', request),

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

export interface ImportCandidate {
  videoId: string;
  title: string;
  publishedAt: string;
  alreadyImported?: boolean;
}

export interface BulkImportRequest {
  items: BulkImportItem[];
}

export interface BulkImportItem {
  videoId: string;
  categories: string[];
  target?: string;
}

export interface BulkImportResult {
  videoId: string;
  status: 'imported' | 'skipped' | 'error';
  error?: string;
}

// Export individual function for convenience
export const publishVideoToSocial = VideoService.publishToSocial;
export const refreshVideoYouTubeData = VideoService.refreshYouTubeData;
