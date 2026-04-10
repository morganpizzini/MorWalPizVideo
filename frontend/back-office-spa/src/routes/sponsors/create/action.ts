import { createSponsorWithImage } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();

  try {
    await createSponsorWithImage(formData);
    return { success: true };
  } catch (error) {
    console.error('Error creating sponsor:', error);
    return { errors: ['Failed to create sponsor'] };
  }
}
