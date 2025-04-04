// Service for interacting with the Video API endpoints

import {
  VideoImportRequest,
  SwapRootThumbnailRequest,
  RootCreationRequest,
  SubVideoCrationRequest,
  ReviewDetails,
} from './types';

// Assuming the API base URL is configured elsewhere
const API_BASE_URL = '/api';

export const VideoService = {
  // Translate video shorts
  translateShort: async (videoIds: string[]): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/Translate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(videoIds),
    });

    if (!response.ok) {
      throw new Error(`Error translating videos: ${response.statusText}`);
    }
  },

  // Import a video
  importVideo: async (request: VideoImportRequest): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/ImportVideo`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Error importing video: ${response.statusText}`);
    }
  },

  // Convert a video to root
  convertToRoot: async (request: RootCreationRequest): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/ConvertIntoRoot`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Error converting video to root: ${response.statusText}`);
    }
  },

  // Swap thumbnail URL
  swapThumbnailUrl: async (request: SwapRootThumbnailRequest): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/SwapThumbnailId`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Error swapping thumbnail: ${response.statusText}`);
    }
  },

  // Create a root video
  createRoot: async (request: RootCreationRequest): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/RootCreation`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Error creating root video: ${response.statusText}`);
    }
  },

  // Create a sub-video
  createSubVideo: async (request: SubVideoCrationRequest): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/Video/ImportSubCreation`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Error creating sub-video: ${response.statusText}`);
    }
  },

  // Get review details
  getReviewDetails: async (reviewText: string): Promise<ReviewDetails> => {
    const response = await fetch(`${API_BASE_URL}/Chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(reviewText),
    });

    if (!response.ok) {
      throw new Error(`Error getting review details: ${response.statusText}`);
    }

    return response.json();
  },
};
