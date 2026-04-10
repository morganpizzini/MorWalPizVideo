import { deleteProduct } from '@morwalpizvideo/services';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  const productId = formData.get('productId') as string;

  try {
    await deleteProduct(productId);
    return { success: true };
  } catch (error: any) {
    return {
      errors: {
        generics: [error.message || 'Failed to delete product'],
      },
    };
  }
}
