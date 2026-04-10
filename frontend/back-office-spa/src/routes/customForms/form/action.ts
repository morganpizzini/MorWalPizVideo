import { ActionFunctionArgs, redirect } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';

export default async function action({ request, params }: ActionFunctionArgs) {
  const { id } = params;
  const formData = await request.formData();
  
  const title = formData.get('title') as string;
  const description = formData.get('description') as string;
  const url = formData.get('url') as string;
  const active = formData.get('active') === 'true';
  const questionsJson = formData.get('questions') as string;

  if (!title) {
    return {
      success: false,
      errors: {
        fields: { title: 'Title is required' },
        generics: []
      }
    };
  }

  if (!url) {
    return {
      success: false,
      errors: {
        fields: { url: 'URL is required' },
        generics: []
      }
    };
  }

  if (!questionsJson) {
    return {
      success: false,
      errors: {
        generics: ['At least one question is required']
      }
    };
  }

  let questions;
  try {
    questions = JSON.parse(questionsJson);
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: ['Invalid questions data']
      }
    };
  }

  const payload = {
    title,
    description: description || '',
    url,
    active,
    questions
  };

  try {
    if (id) {
      // Update existing form
      await put(ComposeUrl(endpoints.CUSTOMFORMS_DETAIL, { customFormId: id }), { ...payload, id });
    } else {
      // Create new form
      await post(endpoints.CUSTOMFORMS, payload);
    }

    // Redirect to the list page on success
    return redirect('/customforms');
  } catch (error) {
    return {
      success: false,
      errors: {
        generics: [(error as Error).message || 'An unexpected error occurred']
      }
    };
  }
}
