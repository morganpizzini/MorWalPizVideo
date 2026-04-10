import { updateSponsorWithImage } from '@morwalpizvideo/services';

export default async function action({ request, params }: { request: Request; params: { id: string } }) {
  const formData = await request.formData();

  try {
    await updateSponsorWithImage(params.id, formData);
    return { success: true };
  } catch (error) {
    console.error('Error updating sponsor:', error);
    return { errors: ['Failed to update sponsor'] };
  }
}
