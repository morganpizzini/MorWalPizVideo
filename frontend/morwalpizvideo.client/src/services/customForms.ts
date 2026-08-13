/**
 * CustomForms API service
 */

import { getActiveCustomForms, getCustomFormByUrl as fetchCustomFormByUrl, submitCustomFormResponse } from '@morwalpizvideo/services';
import type { AnyAnswer, CustomForm, CustomFormResponse } from '@morwalpizvideo/models';

/**
 * Get all active custom forms
 * @returns {Promise<CustomForm[]>} Array of active forms
 */
export async function getActiveForms(): Promise<CustomForm[]> {
  return getActiveCustomForms();
}

/**
 * Get a custom form by URL
 * @param {string} url - The URL of the form
 * @returns {Promise<CustomForm>} The form data
 */
export async function getCustomFormByUrl(url: string): Promise<CustomForm> {
  return fetchCustomFormByUrl(url);
}

/**
 * Submit a response to a form
 * @param {string} formId - The ID of the form
 * @param {Array} answers - Array of answers
 * @returns {Promise<CustomFormResponse>} The response data
 */
export async function submitFormResponse(formId: string, answers: AnyAnswer[]): Promise<CustomFormResponse> {
  return submitCustomFormResponse(formId, answers);
}