/**
 * CustomForms API service
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
 * Get all active custom forms
 * @returns {Promise<Array>} Array of active forms
 */
export async function getActiveForms() {
  const response = await fetch(buildApiUrl('/api/customforms/active'));
  
  if (!response.ok) {
    throw new Error(`Failed to fetch active forms: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Get a custom form by URL
 * @param {string} url - The URL of the form
 * @returns {Promise<Object>} The form data
 */
export async function getCustomFormByUrl(url: string) {
  const response = await fetch(buildApiUrl(`/api/customforms/url/${encodeURIComponent(url)}`));
  
  if (!response.ok) {
    throw new Error(`Failed to fetch form: ${response.statusText}`);
  }
  
  return response.json();
}

/**
 * Submit a response to a form
 * @param {string} formId - The ID of the form
 * @param {Array} answers - Array of answers
 * @returns {Promise<Object>} The response data
 */
export async function submitFormResponse(formId: string, answers: any[]) {
  const response = await fetch(buildApiUrl(`/api/customforms/${encodeURIComponent(formId)}/responses`), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ answers }),
  });
  
  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || 'Failed to submit form response');
  }
  
  return response.json();
}
