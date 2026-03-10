/**
 * CustomForms API service
 */

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

/**
 * Get all active custom forms
 * @returns {Promise<Array>} Array of active forms
 */
export async function getActiveForms() {
  const response = await fetch(`${API_BASE_URL}/api/customforms/active`);
  
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
export async function getCustomFormByUrl(url) {
  const response = await fetch(`${API_BASE_URL}/api/customforms/url/${encodeURIComponent(url)}`);
  
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
export async function submitFormResponse(formId, answers) {
  const response = await fetch(`${API_BASE_URL}/api/customforms/${encodeURIComponent(formId)}/responses`, {
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
