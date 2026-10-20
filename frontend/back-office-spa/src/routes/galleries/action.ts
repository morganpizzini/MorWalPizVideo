import { data } from 'react-router';
import { deleteGallery } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const id = String((await request.formData()).get('id') ?? '');
  if (!id) return data({ errors: ['Gallery ID is required'] }, { status: 400 });
  try {
    await deleteGallery(id);
    return { success: true };
  } catch {
    return data({ errors: ['Failed to delete gallery'] }, { status: 500 });
  }
}
