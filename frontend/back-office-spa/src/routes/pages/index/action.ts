import { data } from 'react-router';
import { deletePage } from '@morwalpizvideo/services';
import { getPagesApiError, getPagesErrorMessages } from '../response';

export default async function action({ request }: { request: Request }) {
  const id = String((await request.formData()).get('id') ?? '');
  if (!id) return data({ errors: ['Page ID is required'] }, { status: 400 });
  try {
    const response = await deletePage(id);
    const error = getPagesApiError(response);
    if (error) return data({ errors: getPagesErrorMessages(response, 'Failed to delete page') }, { status: error.status ?? 500 });
    return { success: true };
  } catch {
    return data({ errors: ['Failed to delete page'] }, { status: 500 });
  }
}