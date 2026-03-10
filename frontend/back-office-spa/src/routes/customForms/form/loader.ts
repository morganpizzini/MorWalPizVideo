import { LoaderFunctionArgs } from 'react-router';
import { CustomForm } from '@morwalpizvideo/models';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;

  // If no ID, this is create mode
  if (!id) {
    return null;
  }

  // Edit mode - load existing form
  try {
    const response = await fetch(`/api/customforms/${encodeURIComponent(id)}`);

    if (!response.ok) {
      throw new Response('Form not found', { status: response.status });
    }

    const form: CustomForm = await response.json();
    return form;
  } catch (error) {
    console.error('Failed to load custom form:', error);
    throw error;
  }
}
