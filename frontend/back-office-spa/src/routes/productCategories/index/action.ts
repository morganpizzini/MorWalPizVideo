import { deleteProductCategory } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const id = formData.get('id') as string;

  if (!id) {
    return { errors: ['Category ID is required'] };
  }

  try {
    await deleteProductCategory(id);
    return { success: true };
  } catch (error) {
    console.error('Error deleting product category:', error);
    return { errors: ['Failed to delete product category'] };
  }
}
