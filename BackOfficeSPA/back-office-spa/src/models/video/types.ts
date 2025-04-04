// Types for Video operations, based on backend controller

/**
 * Interface representing a video in the system
 */
export interface Video {
  id: string;
  title: string;
  description?: string;
  url: string;
  category: string;
}

export interface VideoImportRequest {
  videoId: string;
  category: string;
}

export interface SwapRootThumbnailRequest {
  currentVideoId: string;
  newVideoId: string;
}

export interface RootCreationRequest {
  videoId: string;
  title: string;
  description: string;
  url: string;
  category: string;
}

export interface SubVideoCrationRequest {
  matchId: string;
  videoId: string;
  category: string;
}

export interface VideoTranslateRequest {
  videoIds: string[];
}

export interface ReviewDetails {
  titleItalian: string;
  titleEnglish: string;
}

// Export a common type for dropdown categories if needed
export type VideoCategory = string;
