/**
 * API Keys management service
 */

import { Delete, get, post, put, frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

/**
 * Get all API keys
 * @returns {Promise<Array>} Array of API keys
 */
export async function getAllApiKeys(): Promise<any[]> {
  return get(frontendEndpoints.APIKEYS);
}

/**
 * Get API key by ID
 * @param {string} id - API key ID
 * @returns {Promise<Object>} API key details
 */
export async function getApiKeyById(id: string): Promise<any> {
  return get(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id }));
}

/**
 * Create a new API key
 * @param {Object} data - API key data
 * @param {string} data.name - Key name (required)
 * @param {string} [data.description] - Key description
 * @param {number} [data.rateLimitPerMinute] - Rate limit
 * @param {Array<string>} [data.allowedIpAddresses] - Allowed IPs
 * @param {string} [data.expiresAt] - Expiration date (ISO string)
 * @returns {Promise<Object>} Created API key with unhashed key
 */
export async function createApiKey(data: {
  name: string;
  description?: string;
  rateLimitPerMinute?: number;
  allowedIpAddresses?: string[];
  expiresAt?: string;
}): Promise<any> {
  return post(frontendEndpoints.APIKEYS, data);
}

/**
 * Update an existing API key
 * @param {string} id - API key ID
 * @param {Object} data - Updated data
 * @param {string} [data.name] - Key name
 * @param {string} [data.description] - Key description
 * @param {number} [data.rateLimitPerMinute] - Rate limit
 * @param {Array<string>} [data.allowedIpAddresses] - Allowed IPs
 * @param {string} [data.expiresAt] - Expiration date (ISO string)
 * @returns {Promise<Object>} Response message
 */
export async function updateApiKey(id: string, data: {
  name?: string;
  description?: string;
  rateLimitPerMinute?: number;
  allowedIpAddresses?: string[];
  expiresAt?: string;
}): Promise<any> {
  return put(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id }), data);
}

/**
 * Toggle API key active status
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response with new status
 */
export async function toggleApiKey(id: string): Promise<any> {
  return post(ComposeUrl(frontendEndpoints.APIKEYS_TOGGLE, { id }), {});
}

/**
 * Regenerate API key
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response with new unhashed key
 */
export async function regenerateApiKey(id: string): Promise<any> {
  return post(ComposeUrl(frontendEndpoints.APIKEYS_REGENERATE, { id }), {});
}

/**
 * Delete an API key
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response message
 */
export async function deleteApiKey(id: string): Promise<any> {
  return Delete(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id }));
}