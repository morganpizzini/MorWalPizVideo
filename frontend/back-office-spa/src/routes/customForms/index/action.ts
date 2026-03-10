import { ActionFunctionArgs } from 'react-router';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const id = formData.get('id');

  if (!id) {
    return {
      success: false,
      errors: {
        generics: ['Custom form ID is required']
      }
    };
  }

  try {
    const response = await fetch(`/api/customforms/${encodeURIComponent(id.toString())}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        errors: {
          generics: [errorText || 'Failed to delete custom form']
        }
      };
    }

    return {
      success: true
    };
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: [(error as Error).message || 'An unexpected error occurred']
      }
    };
  }
}
