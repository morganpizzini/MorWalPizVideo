/**
 * CustomForms API service
 */

import { get, post, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { CustomForm, CustomFormResponse } from '@morwalpizvideo/models';

/**
 * Get all active custom forms
 * @returns {Promise<CustomForm[]>} Array of active forms
 */
export async function getActiveForms(): Promise<CustomForm[]> {
  return get(endpoints.CUSTOMFORMS_ACTIVE);
}

/**
 * Get a custom form by URL
 * @param {string} url - The URL of the form
 * @returns {Promise<CustomForm>} The form data
 */
export async function getCustomFormByUrl(url: string): Promise<CustomForm> {
  return get(ComposeUrl(endpoints.CUSTOMFORMS_BY_URL, { url }));
}

/**
 * Submit a response to a form
 * @param {string} formId - The ID of the form
 * @param {Array} answers - Array of answers
 * @returns {Promise<CustomFormResponse>} The response data
 */
export async function submitFormResponse(formId: string, answers: any[]): Promise<CustomFormResponse> {
  return post(ComposeUrl(endpoints.CUSTOMFORMS_RESPONSES, { customFormId: formId }), { answers });
}