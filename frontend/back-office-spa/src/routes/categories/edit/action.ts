import { ActionFunctionArgs, data } from 'react-router';

import { UpdateCategoryDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as UpdateCategoryDTO;
  const errors: Record<string, string | string[]> = {};

  // Validate fields
  if (!values.title || values.title.trim().length === 0) {
    errors['title'] = 'Title cannot be empty';
  }

  if (!values.description || values.description.trim().length === 0) {
    errors['description'] = 'Description cannot be empty';
  }

  // Check for errors
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  // If no errors, execute API request
  return fetch(`/api/categories/${params.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
    .then(() => {
      return data({ success: true }, { status: 200 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
