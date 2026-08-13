import { deleteQuickLinks } from '@morwalpizvideo/services';
import { data } from 'react-router';
import { getQuickLinksApiError, getQuickLinksErrorMessages } from '../response';

export default async function action({ request }: { request: Request }) {
  const id = (await request.formData()).get('id') as string;
  if (!id) return { errors: ['QuickLinks ID is required'] };

  try {
    const response = await deleteQuickLinks(id);
    const apiError = getQuickLinksApiError(response);
    if (apiError) {
      return data({ errors: getQuickLinksErrorMessages(response, 'Failed to delete QuickLinks') }, { status: apiError.status ?? 500 });
    }
    return { success: true };
  } catch {
    return { errors: ['Failed to delete QuickLinks'] };
  }
}
