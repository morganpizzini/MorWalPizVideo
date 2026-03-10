import { data } from 'react-router';

export default async function action({ request, params }: { request: Request; params: any }) {
  const formData = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  // Determine if this is create or edit mode
  const isEditMode = !!params.id;

  // Parse form values
  const values: any = {
    title: String(formData.title || ''),
    description: String(formData.description || ''),
    url: String(formData.url || ''),
    videos: formData.videos ? JSON.parse(String(formData.videos)) : [],
  };

  // Ensure videos is an array of strings (video IDs)
  if (Array.isArray(values.videos) && values.videos.length > 0) {
    // If videos are objects with youtubeId, extract the IDs
    if (typeof values.videos[0] === 'object' && values.videos[0].youtubeId) {
      values.videos = values.videos.map((v: any) => v.youtubeId);
    }
  }

  // Validation
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title is required';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description is required';
  }

  if (!values.url || values.url.trim().length === 0) {
    errors['url'] = 'URL is required';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // Prepare API request
  const apiUrl = isEditMode ? `/api/compilations/${params.id}` : '/api/compilations';
  const method = isEditMode ? 'PUT' : 'POST';

  // If editing, include the ID in the payload
  if (isEditMode) {
    values.id = params.id;
  }

  return fetch(apiUrl, {
    method: method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(response => {
      if (!response.ok) {
        throw new Error('API request failed');
      }
      return data({ success: true }, { status: isEditMode ? 200 : 201 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
