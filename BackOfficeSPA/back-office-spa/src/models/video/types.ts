// Types for Video operations, based on backend controller

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
 * Interface representing a video in the system
 */
export interface Video {
  youtubeId: string;
  title: string;
  description?: string;
  thumbnail?: string;
  duration?: string;
  views?: number;
  likes?: number;
  comments?: number;
  publishedAt?: string;
  category: string;
}

/**
 * Interface representing a match in the system
 */
export interface Match {
  id: string;
  matchId: string;
  title: string;
  description?: string;
  url: string;
  thumbnailVideoId: string; // Previously referred to as thumbnailUrl
  videoRefs: VideoRef[];
  category: string;
  matchType: MatchType;
  // Backward compatibility
  isLink: boolean;
  videos?: Video[];
  creationDateTime?: string;
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
