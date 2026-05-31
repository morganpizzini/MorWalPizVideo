import type { LoaderFunctionArgs } from 'react-router';
import { getCustomFormByUrl } from '../services/customForms';

/**
 * Loader for custom form route
 */
export default async function customFormLoader({ params }: LoaderFunctionArgs) {
  const { formUrl } = params;
  
  if (!formUrl) {
    throw new Error('Form URL is required');
  }
  
  try {
    const form = await getCustomFormByUrl(formUrl);
    return { form };
  } catch (error) {
    console.error('Error loading custom form:', error);
    throw error;
  }
}
