import { deleteSponsor } from '@services/apiService';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
    const id = formData.get('sponsorId') as string;

  try {
    await deleteSponsor(id);
    return { success: true };
  } catch (error) {
    console.error('Error deleting sponsor:', error);
    return { errors: ['Failed to delete sponsor'] };
  }
}
