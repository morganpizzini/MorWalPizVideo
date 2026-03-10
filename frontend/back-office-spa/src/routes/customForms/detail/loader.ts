import { LoaderFunctionArgs } from 'react-router';
import { CustomForm } from '@morwalpizvideo/models';

export default async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;

  if (!id) {
    throw new Response('Form ID is required', { status: 400 });
  }

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
