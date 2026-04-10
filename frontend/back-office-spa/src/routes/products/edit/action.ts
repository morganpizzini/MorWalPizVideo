import { updateProduct } from '@morwalpizvideo/services';
import type { UpdateProductDTO } from '@morwalpizvideo/models';

export default async function action({ request, params }: { request: Request; params: { productId: string } }) {
  const formData = await request.formData();
  
  const title = formData.get('title') as string;
  const description = formData.get('description') as string;
  const url = formData.get('url') as string;
  const categoryIdsJson = formData.get('categoryIds') as string;
  
  const categoryIds = JSON.parse(categoryIdsJson || '[]');

  const productData: UpdateProductDTO = {
    title,
    description,
    url,
    categoryIds,
  };

  try {
    await updateProduct(params.productId, productData);
    return { success: true };
  } catch (error: any) {
    return {
      errors: {
        generics: [error.message || 'Failed to update product'],
      },
    };
  }
}
