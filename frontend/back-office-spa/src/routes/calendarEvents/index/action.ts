import { ActionFunctionArgs } from 'react-router';
import { Delete, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const title = formData.get('title');

  if (!title) {
    return {
      success: false,
      errors: {
        generics: ['Calendar event title is required']
      }
    };
  }

  try {
    const result = await Delete(ComposeUrl(endpoints.CALENDAREVENTS_DETAIL, { title: title.toString() }));
    
    if (result?.errors) {
      return {
        success: false,
        errors: result.errors
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