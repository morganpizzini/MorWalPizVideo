/**
 * API Keys management service
 */

/**
 * Get the API base URL from runtime environment or build-time environment
 * Priority: window.ENV (Docker runtime) > import.meta.env (Vite build-time) > relative paths
 */
function getApiBaseUrl(): string {
  // Check runtime environment (injected by Docker entrypoint)
  if (typeof window !== 'undefined' && window.ENV?.VITE_API_BASE_URL) {
    return window.ENV.VITE_API_BASE_URL;
  }
  if (typeof window !== 'undefined' && window.ENV?.API_BASE_URL) {
    return window.ENV.API_BASE_URL;
  }
  if (typeof window !== 'undefined' && window.ENV?.REACT_APP_API_URL) {
    return window.ENV.REACT_APP_API_URL;
  }
  
  // Check build-time environment (Vite)
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  if (import.meta.env.VITE_API_BASE_URL) {
    return import.meta.env.VITE_API_BASE_URL;
  }
  
  // Default to empty string for relative paths
  return '';
}

/**
 * Build full API URL with proper normalization
 * Handles both '/api/...' and 'api/...' formats safely
 */
function buildApiUrl(path: string): string {
  const baseUrl = getApiBaseUrl();
  
  // Normalize path to have leading slash
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  
  // If no base URL, return relative path
  if (!baseUrl) {
    return normalizedPath;
  }
  
  // Remove trailing slash from base URL
  const cleanBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
  
  // Combine base URL with path
  return `${cleanBase}${normalizedPath}`;
}

/**
 * Get authentication token from localStorage
 * @returns {string|null} JWT token
 */
function getAuthToken() {
  return localStorage.getItem('authToken');
}

/**
 * Create headers with authentication
 * @returns {Object} Headers object
 */
function getAuthHeaders() {
  const token = getAuthToken();
  return {
    'Content-Type': 'application/json',
    ...(token && { 'Authorization': `Bearer ${token}` })
  };
}

/**
 * Get all API keys
 * @returns {Promise<Array>} Array of API keys
 */
export async function getAllApiKeys() {
  const response = await fetch(buildApiUrl('/api/apikeys'), {
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
export async function getApiKeyById(id) {
  const response = await fetch(buildApiUrl(`/api/apikeys/${encodeURIComponent(id)}`), {
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
export async function createApiKey(data) {
  const response = await fetch(buildApiUrl('/api/apikeys'), {
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
export async function updateApiKey(id, data) {
  const response = await fetch(buildApiUrl(`/api/apikeys/${encodeURIComponent(id)}`), {
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
export async function toggleApiKey(id) {
  const response = await fetch(buildApiUrl(`/api/apikeys/${encodeURIComponent(id)}/toggle`), {
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
export async function regenerateApiKey(id) {
  const response = await fetch(buildApiUrl(`/api/apikeys/${encodeURIComponent(id)}/regenerate`), {
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
export async function deleteApiKey(id) {
  const response = await fetch(buildApiUrl(`/api/apikeys/${encodeURIComponent(id)}`), {
    method: 'DELETE',
    headers: getAuthHeaders()
  });
  
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.message || `Failed to delete API key: ${response.statusText}`);
  }
  
  return response.json();
}