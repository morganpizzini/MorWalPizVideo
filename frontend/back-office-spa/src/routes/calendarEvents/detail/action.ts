import { ActionFunctionArgs } from 'react-router';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();
  const title = formData.get('title') || params.title;

  if (!title) {
    return {
      success: false,
      errors: {
        generics: ['Calendar event title is required']
      }
    };
  }

  try {
    const response = await fetch(`/api/calendarEvents/${encodeURIComponent(title.toString())}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        errors: {
          generics: [errorText || 'Failed to delete calendar event']
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
