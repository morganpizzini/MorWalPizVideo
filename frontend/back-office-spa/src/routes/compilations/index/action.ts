import { data } from 'react-router';
import { Delete } from '@services/apiService';
import endpoints, { ComposeUrl } from '@services/endpoints';

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
  try {
    await Delete(ComposeUrl(endpoints.COMPILATIONS_DETAIL, { compilationId: id }));
    return data({ success: true }, { status: 200 });
  } catch (error) {
    errors['generics'] = ['API error found'];
    return data({ success: false, errors }, { status: 500 });
  }
}
