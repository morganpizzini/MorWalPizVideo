/**
 * API Keys management service
 */

import { frontendEndpoints, ComposeUrl } from '@morwalpizvideo/services';

/**
 * Get authentication token from localStorage
 * @returns {string|null} JWT token
 */
function getAuthToken(): string | null {
  return localStorage.getItem('authToken');
}

/**
 * Create headers with authentication
 * @returns {Object} Headers object
 */
function getAuthHeaders(): HeadersInit {
  const token = getAuthToken();
  return {
    'Content-Type': 'application/json',
    ...(token && { 'Authorization': `Bearer ${token}` })
  };
}

/**
 * Helper to build full URL from endpoint using ComposeUrl
 * ComposeUrl handles base URL resolution and environment variables
 */
function buildUrl(endpoint: string): string {
  return ComposeUrl(endpoint, {});
}

/**
 * Get all API keys
 * @returns {Promise<Array>} Array of API keys
 */
export async function getAllApiKeys(): Promise<any[]> {
  const response = await fetch(buildUrl(frontendEndpoints.APIKEYS), {
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch API keys: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get API key by ID
 * @param {string} id - API key ID
 * @returns {Promise<Object>} API key details
 */
export async function getApiKeyById(id: string): Promise<any> {
  const response = await fetch(buildUrl(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id })), {
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    if (response.status === 404) {
      throw new Error('API key not found');
    }
    throw new Error(`Failed to fetch API key: ${response.statusText}`);
  }
  
  return response.json();
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
  const response = await fetch(buildUrl(frontendEndpoints.APIKEYS), {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(data)
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to create API key: ${response.statusText}`);
  }
  
  return response.json();
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
  const response = await fetch(buildUrl(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id })), {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(data)
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to update API key: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Toggle API key active status
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response with new status
 */
export async function toggleApiKey(id: string): Promise<any> {
  const response = await fetch(buildUrl(ComposeUrl(frontendEndpoints.APIKEYS_TOGGLE, { id })), {
    method: 'POST',
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to toggle API key: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Regenerate API key
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response with new unhashed key
 */
export async function regenerateApiKey(id: string): Promise<any> {
  const response = await fetch(buildUrl(ComposeUrl(frontendEndpoints.APIKEYS_REGENERATE, { id })), {
    method: 'POST',
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to regenerate API key: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Delete an API key
 * @param {string} id - API key ID
 * @returns {Promise<Object>} Response message
 */
export async function deleteApiKey(id: string): Promise<any> {
  const response = await fetch(buildUrl(ComposeUrl(frontendEndpoints.APIKEYS_DETAIL, { id })), {
    method: 'DELETE',
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to delete API key: ${response.statusText}`);
  }
  
  return response.json();
}