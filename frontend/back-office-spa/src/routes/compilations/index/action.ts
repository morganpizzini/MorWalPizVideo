import { data } from 'react-router';

export default async function action({ request }: { request: Request }) {
  const formData = Object.fromEntries(await request.formData());
  const errors: Record<string, string | string[]> = {};

  const id = String(formData.id || '');

  // Validation
  if (!id || id.trim().length === 0) {
    errors['generics'] = ['ID is required'];
    return data({ success: false, errors }, { status: 400 });
  }

  // If no errors, proceed with API request
  return fetch(`/api/compilations/${id}`, {
    method: 'DELETE',
  })
    .then(() => {
      return data({ success: true }, { status: 200 });
    })
    .catch(() => {
      errors['generics'] = ['API error found'];
      return data({ success: false, errors }, { status: 500 });
    });
}
