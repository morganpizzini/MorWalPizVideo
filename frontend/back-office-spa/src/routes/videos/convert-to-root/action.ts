import { ActionFunctionArgs, data } from 'react-router';


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
  // Prepare request with single category
  const category = values.categories.length > 0 ? values.categories[0] : '';
  
  // API request - updated for new Match structure
  return fetch(`/api/videos/ConvertIntoRoot`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      videoId: values.videoId,
      title: values.title,
      description: values.description,
      url: values.url,
      category: category
    }),
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
