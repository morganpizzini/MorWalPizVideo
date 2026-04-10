/**
 * Insights feature types for news discovery and content planning
 */

export enum InsightNewsStatus {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
  Generated = 3,
}

export enum ContentPlanType {
  Article = 0,
  Podcast = 1,
  Video = 2,
  SocialPost = 3,
  Newsletter = 4,
}

export interface InsightTopic {
  id: string;
  title: string;
  description: string;
  seedArguments: string[];
  preferredSources: string[];
  creationDateTime: string;
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