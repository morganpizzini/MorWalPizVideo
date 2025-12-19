import { ActionFunctionArgs, data } from 'react-router';


export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const categoriesFromForm = formData.get('categories');
  const categories = categoriesFromForm ? JSON.parse(categoriesFromForm as string) : [];

  const values = {
    videoId: formData.get('videoId') as string,
    matchId: formData.get('matchId') as string,
    categories: categories,
  };

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!values.videoId || values.videoId.trim().length === 0) {
    errors['videoId'] = 'Video ID cannot be empty';
  }

  if (!values.matchId || values.matchId.trim().length === 0) {
    errors['matchId'] = 'Match ID cannot be empty';
  }

  if (!values.categories || values.categories.length === 0) {
    errors['categories'] = 'At least one category must be selected';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }
  // Prepare request with single category
  const category = values.categories.length > 0 ? values.categories[0] : '';
  
  // API request - updated for new Match structure using AddVideo method
  return fetch(`/api/videos/ImportSubCreation`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      matchId: values.matchId,
      videoId: values.videoId,
      category: category
    }),
  })
    .then(async response => {
      if (!response.ok) {
        const errorData = await response.json();
        errors['generics'] = [errorData.message || 'Failed to import sub video'];
        return data({ success: false, errors }, { status: response.status });
      }
      return data({ success: true }, { status: 201 });
    })
    .catch(error => {
      errors['generics'] = [error.message || 'API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
