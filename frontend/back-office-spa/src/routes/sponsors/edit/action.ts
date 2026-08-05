import { updateSponsorWithImage } from '@morwalpizvideo/services';
import type { ActionFunctionArgs } from 'react-router';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();

  try {
    await updateSponsorWithImage(params.id!, formData);
    return { success: true };
  } catch (error) {
    console.error('Error updating sponsor:', error);
    return { errors: ['Failed to update sponsor'] };
  }
}
