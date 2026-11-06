export enum BlogCommentStatus {
  Pending,
  Approved,
  Rejected,
  Hidden,
  Withdrawn,
}

export enum BlogReactionValue {
  Like,
  Dislike,
}

export interface BlogInteractionSummary {
  likes: number;
  dislikes: number;
  approvedComments: number;
}

export interface BlogComment {
  id: string;
  displayName: string;
  body: string;
  createdAt: string;
}

export interface BlogCommentPage {
  items: BlogComment[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BlogViewerState {
  commentsBlocked: boolean;
  portalAccessRevoked: boolean;
  reaction: BlogReactionValue | null;
}

export interface BlogCommentSubmission {
  body: string;
  requestId: string;
}

export interface BlogReactionRequest {
  value: "Like" | "Dislike";
}

export interface BlogAdminComment {
  id: string;
  accountId: string;
  displayName: string;
  postId: string;
  slug: string;
  body: string;
  status: BlogCommentStatus;
  revision: number;
  createdAt: string;
  moderatedBy: string | null;
  moderatedAt: string | null;
}

export interface BlogAdminCommentPage {
  items: BlogAdminComment[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BlogArticleInteractions {
  postId: string;
  slug: string;
  title: string;
  published: boolean;
  likes: number;
  dislikes: number;
  comments: number;
}

export interface BlogArticleInteractionsPage {
  items: BlogArticleInteractions[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface BlogPortalAccess {
  channelId: string;
  accountId: string;
  commentsBlocked: boolean;
  portalAccessRevoked: boolean;
  revision: number;
  reason: string;
  updatedBy: string;
  updatedAt: string | null;
}

export interface BlogPortalAccessRequest {
  revision: number;
  commentsBlocked: boolean;
  portalAccessRevoked: boolean;
  reason: string;
}

export interface PublicAccountDirectoryItem {
  id: string;
  username: string;
  displayName: string;
  isActive: boolean;
  createdAt: string;
}

export interface PublicAccountDirectoryPage {
  items: PublicAccountDirectoryItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PublicAccountAdminDetail extends PublicAccountDirectoryItem {
  portalAccess: BlogPortalAccess;
  commentCount: number;
  likeCount: number;
  dislikeCount: number;
}

export interface BlogReactionAdmin {
  postId: string;
  slug: string;
  value: BlogReactionValue;
  createdAt: string;
}

export interface PublicAccountInteractionDetail {
  account: PublicAccountAdminDetail;
  comments: BlogAdminComment[];
  reactions: BlogReactionAdmin[];
  page: number;
  pageSize: number;
}
