export type LegalContentType = 'terms' | 'privacy' | 'cookie-policy';

export interface LegalContent {
  type: LegalContentType;
  content: string;
  lastUpdated: Date;
}

export interface CreateLegalContentRequest {
  type: LegalContentType;
  content: string;
}

export interface UpdateLegalContentRequest {
  content: string;
}
