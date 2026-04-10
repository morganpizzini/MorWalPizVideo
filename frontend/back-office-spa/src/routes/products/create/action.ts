import { createProduct } from '@morwalpizvideo/services';
import type { CreateProductDTO } from '@morwalpizvideo/models';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  
  const title = formData.get('title') as string;
  const description = formData.get('description') as string;
  const url = formData.get('url') as string;
  const categoryIdsJson = formData.get('categoryIds') as string;
  
  const categoryIds = JSON.parse(categoryIdsJson || '[]');

  const productData: CreateProductDTO = {
    title,
    description,
    url,
    categoryIds,
  };

  try {
    await createProduct(productData);
    return { success: true };
  } catch (error: any) {
    return {
      errors: {
        generics: [error.message || 'Failed to create product'],
      },
    };
  }
}
