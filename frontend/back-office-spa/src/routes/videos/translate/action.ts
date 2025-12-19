import { ActionFunctionArgs, data } from 'react-router';


export default async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const videoIds = formData.getAll('videoIds') as string[];

  const errors: Record<string, string | string[]> = {};

  // Field validation
  if (!videoIds || videoIds.length === 0 || videoIds.every(id => !id || id.trim().length === 0)) {
    errors['videoIds'] = 'At least one valid Video ID must be selected';
  }

  // Return errors if any
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // Filter out any empty values
  const cleanVideoIds = videoIds.filter(id => id && id.trim().length > 0);

  // API request
  return fetch(`/api/videos/translate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(cleanVideoIds),
  })
    .then(async response => {
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        errors['generics'] = [errorData.message || 'Failed to translate videos'];
        return data({ success: false, errors }, { status: response.status });
      }
      return data({ success: true }, { status: 200 });
    })
    .catch(error => {
      errors['generics'] = [error.message || 'API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
