/**
 * Insights feature types for news discovery and content planning
 */

export enum InsightNewsStatus {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  Generated = 3,
  AutoDetected = 4,
}

export enum ContentPlanType {
  Article = 0,
  Podcast = 1,
  SocialPost = 2,
  VideoScript = 3,
  Newsletter = 4,
}

export enum InsightSourceKind {
  Content = 0,
  ShortContent = 1,
}

export enum InsightTopicCreationMode {
  General = 0,
  YouTubeCommentAnalysis = 1,
}

export enum InsightCommentAnalysisRunStatus {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Rejected = 3,
}

export interface InsightTopic {
  id: string;
  title: string;
  description: string;
  seedArguments: string[];
  preferredSources: string[];
  creationDateTime: string;
  creationMode?: InsightTopicCreationMode;
}

export interface InsightNewsItem {
  id: string;
  topicId: string;
  title: string;
  summary: string;
  sourceUrl: string;
  sourceName: string;
  status: InsightNewsStatus;
  starRating: number;
  aiRelevanceScore: number;
  discoveredAt: string;
  rankingScore: number;
  creationDateTime: string;
  platformSource: string;
  postId: string;
  videoId: string;
  analysisReason: string;
  reviewReason: string;
  sourceKind: InsightSourceKind;
  commentExcerpt: string;
  sentiment: string;
  sourceComments?: InsightSourceComment[];
}

export interface InsightSourceComment {
  fullText: string;
  highlightText: string;
  author: string;
  publishedAt: string;
}

export interface InsightContentPlan {
  id: string;
  topicId: string;
  title: string;
  type: ContentPlanType;
  outline: string;
  generatedFromNewsItemIds: string[];
  targetPlatforms: string[];
  generatedAt: string;
  creationDateTime: string;
}

// Request DTOs
export interface CreateInsightTopicRequest {
  title: string;
  description: string;
  seedArguments?: string[];
  preferredSources?: string[];
  creationMode?: InsightTopicCreationMode;
}

export interface UpdateInsightTopicRequest {
  title?: string;
  description?: string;
  seedArguments?: string[];
  preferredSources?: string[];
}

export interface ReviewNewsItemRequest {
  status?: InsightNewsStatus;
  starRating?: number;
  reason?: string;
}

export interface GenerateContentPlanRequest {
  topicId: string;
  newsItemIds: string[];
  contentType: ContentPlanType;
  targetPlatforms: string[];
}

export interface UpdateContentPlanRequest {
  title?: string;
  outline?: string;
  targetPlatforms?: string[];
}

export enum InsightCommentSourceType {
  StoredChannel = 0,
  StoredVideo = 1,
  DirectVideoId = 2,
}

export interface AnalyzeInsightCommentsRequest {
  sourceType: InsightCommentSourceType;
  sourceKind?: InsightSourceKind;
  channelId?: string;
  videoId?: string;
  commentsNumber: number;
  excludeUploaderComments?: boolean;
}

export interface AnalyzeInsightCommentsResponse {
  runId: string;
  status: InsightCommentAnalysisRunStatus;
  queued: boolean;
  videosProcessed: number;
  commentsAnalyzed: number;
  createdNewsItemIds: string[];
  errors: string[];
  createdNewsItemCount: number;
  rejectionReason: string;
}