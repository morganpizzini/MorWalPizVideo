import { ActionFunctionArgs, redirect } from 'react-router';

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
    let response;
    
    if (id) {
      // Update existing form
      response = await fetch(`/api/customforms/${encodeURIComponent(id)}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ ...payload, id }),
      });
    } else {
      // Create new form
      response = await fetch('/api/customforms', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });
    }

    if (!response.ok) {
      const errorText = await response.text();
      return {
        success: false,
        errors: {
          generics: [errorText || `Failed to ${id ? 'update' : 'create'} custom form`]
        }
      };
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
