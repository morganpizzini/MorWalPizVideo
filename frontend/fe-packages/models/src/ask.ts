export type AskStatus = 'Draft' | 'Published' | 'Closed' | 'Archived';
export type AskModerationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Spam' | 'Deleted';
export interface AskPolicy { maxSubmissionLength: number; retentionDays: number; rateLimitPerHour: number; duplicateWindowMinutes: number; moderationMode: number; recaptchaRequired: boolean; allowNamedSubmissions: boolean; nameRequired: boolean; }
export interface AskCampaign { id: string; channelId: string; title: string; description: string; slug: string; status: AskStatus | number; policy: AskPolicy; submissionCount: number; publishedAt?: string; closedAt?: string; startAt?: string; endAt?: string; }
export interface AskCampaignRequest { title: string; description: string; slug: string; status: AskStatus | number; policy: AskPolicy; startAt?: string; endAt?: string; }
export interface AskSubmission { id: string; campaignId: string; text: string; name: string; moderationStatus: AskModerationStatus | number; submittedAt: string; responseContent?: string; responseAuthor?: string; responseCreatedAt?: string; responseVisibility?: number; reactionCount: number; moderationNote?: string; }
export interface AskPublicResponse { content: string; author: string; createdAt: string; }
export interface AskReaction { accepted: boolean; count: number; }