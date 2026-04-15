/**
 * API Key TypeScript models
 * Maps to DTOs from MorWalPizVideo.BackOffice/Controllers/ApiKeysController.cs
 */

export interface ApiKeyDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  rateLimitPerMinute: number;
  allowedIpAddresses: string[];
  lastUsedAt: string | null;
  expiresAt: string | null;
  createdAt: string;
}

export interface CreateApiKeyRequest {
  name: string;
  description?: string;
  rateLimitPerMinute?: number;
  allowedIpAddresses?: string[];
  expiresAt?: string;
}

export interface CreateApiKeyResponse {
  id: string;
  name: string;
  description: string;
  key: string; // ONLY returned once on creation
  rateLimitPerMinute: number;
  allowedIpAddresses: string[];
  expiresAt: string | null;
  createdAt: string;
  message: string;
}

export interface UpdateApiKeyRequest {
  name?: string;
  description?: string;
  rateLimitPerMinute?: number;
  allowedIpAddresses?: string[];
  expiresAt?: string;
}

export interface ToggleApiKeyResponse {
  message: string;
  isActive: boolean;
}

export interface RegenerateApiKeyResponse {
  message: string;
  key: string; // ONLY returned once on regeneration
  warning: string;
}