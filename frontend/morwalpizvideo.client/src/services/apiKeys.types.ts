export interface ApiKey {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  expiresAt?: string | null;
  rateLimitPerMinute: number;
  lastUsedAt?: string | null;
  createdAt: string;
  allowedIpAddresses?: string[];
}

export interface GeneratedApiKey extends ApiKey {
  key: string;
}

export interface ToggleResult {
  isActive: boolean;
  message: string;
}

export interface DeleteResult {
  message: string;
}
