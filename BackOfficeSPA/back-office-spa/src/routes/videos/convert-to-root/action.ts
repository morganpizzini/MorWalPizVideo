import { ActionFunctionArgs, data } from 'react-router';
import { API_CONFIG } from '@config/api';

interface ConvertToRootValues {
  videoId: string;
  title: string;
  description: string;
  url: string;
  categories: string[];
}

export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();

  // Parse the categories from JSON string back to array
  let categoriesArray: string[] = [];
  try {
    const categoriesJson = formData.get('categories') as string;
    categoriesArray = JSON.parse(categoriesJson);
  } catch (e) {
    console.error('Error parsing categories:', e);
  }

  const values: ConvertToRootValues = {
    videoId: formData.get('videoId') as string,
    title: formData.get('title') as string,
    description: formData.get('description') as string,
    url: formData.get('url') as string,
    categories: categoriesArray,
  };

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!values.videoId || values.videoId.trim().length === 0) {
    errors['videoId'] = 'Video ID cannot be empty';
  }

  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description cannot be empty';
  }

  if (!values.url || values.url.trim().length === 0) {
    errors['url'] = 'URL cannot be empty';
  }

  if (!values.categories || values.categories.length === 0) {
    errors['categories'] = 'At least one category must be selected';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // API request
  return fetch(`${API_CONFIG.BASE_URL}/videos/ConvertIntoRoot`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(async response => {
      if (!response.ok) {
        const errorData = await response.json();
        errors['generics'] = [errorData.message || 'Failed to convert into root video'];
        return data({ success: false, errors }, { status: response.status });
      }
      return data({ success: true }, { status: 201 });
    })
    .catch(error => {
      errors['generics'] = [error.message || 'API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
