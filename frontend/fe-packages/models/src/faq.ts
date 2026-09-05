export type FaqLifecycleStatus = 0 | 1 | 2;
export type FaqCandidateStatus = 0 | 1 | 2;

export interface FaqSourceMetadata {
  sourceType: string;
  campaignIds: string[];
  generatedAt?: string;
  provider: string;
}

export interface FaqAdmin {
  id: string;
  question: string;
  categoryId: string;
  status: FaqLifecycleStatus | number;
  publishedAt?: string;
  updatedAt: string;
  sourceMetadata: FaqSourceMetadata;
}

export interface FaqCategoryAdmin {
  id: string;
  slug: string;
  name: string;
  description: string;
  sortOrder: number;
  isActive: boolean;
}

export interface FaqAnswerAdmin {
  id: string;
  faqId: string;
  channelId: string;
  content: string;
  status: FaqLifecycleStatus | number;
  helpfulVotes: number;
  notHelpfulVotes: number;
}

export interface FaqCandidateAdmin {
  id: string;
  question: string;
  answer: string;
  campaignIds: string[];
  relevance: number;
  duplicateOfFaqId?: string;
  status: FaqCandidateStatus | number;
  failureReason?: string;
  createdBy: string;
}