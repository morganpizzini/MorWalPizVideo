export type FeedbackStatus = "New" | "Reviewing" | "Resolved" | "Dismissed";

export interface FeedbackContextRequest {
  application: string;
  pagePath: string;
  componentKey?: string;
  clientRelease?: string;
  userAgent?: string;
  language?: string;
  timezone?: string;
  viewportWidth?: number;
  viewportHeight?: number;
  online?: boolean;
}

export interface SubmitFeedbackRequest {
  requestId: string;
  description: string;
  context: FeedbackContextRequest;
}

export interface FeedbackReceipt {
  id: string;
  receivedAtUtc: string;
  authenticated: boolean;
}

export interface FeedbackItem {
  id: string;
  requestId: string;
  application: string;
  description: string;
  pagePath: string;
  componentKey?: string;
  clientRelease?: string;
  userAgent?: string;
  language?: string;
  timezone?: string;
  viewportWidth?: number;
  viewportHeight?: number;
  online?: boolean;
  reporterUsername?: string;
  status: FeedbackStatus;
  revision: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  expiresAtUtc: string;
}

export interface FeedbackPage {
  items: FeedbackItem[];
  page: number;
  pageSize: number;
  total: number;
}

export interface UpdateFeedbackStatusRequest {
  status: FeedbackStatus;
  expectedRevision: number;
}
