// Types for Video operations, based on backend controller

import { ShortLink } from '../shortLink';

/**
 * Enum representing the type of match in the system
 */
export enum ContentType {
  SingleVideo = 0,
  Collection = 1
}

/**
 * Interface representing a category reference
 */
export interface CategoryRef {
  id: string;
  title: string;
}

/**
 * Interface representing a lightweight reference to a video
 */
export interface VideoRef {
  youtubeId: string;
  categories: CategoryRef[];
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
  categories: CategoryRef[];
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
  categories: CategoryRef[];
  contentType: ContentType;
  // Backward compatibility
  isLink: boolean;
  videos?: Video[];
  creationDateTime?: string;
  shortLinks?: ShortLink[];
}

export interface VideoImportRequest {
  videoId: string;
  categories: string[];
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
  categories: string[];
}

export interface SubVideoCrationRequest {
  matchId: string;
  videoId: string;
  categories: string[];
}

export interface Compilation {
  id?: string;
  title: string;
  description: string;
  url: string;
  videos: VideoRef[];
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
