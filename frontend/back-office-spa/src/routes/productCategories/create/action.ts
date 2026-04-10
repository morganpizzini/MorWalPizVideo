import { createProductCategory } from '@morwalpizvideo/services';
import type { CreateProductCategoryDTO } from '@morwalpizvideo/models';

export default async function action({ request }: { request: Request }) {
  const formData = await request.formData();
  
  const data: CreateProductCategoryDTO = {
    title: formData.get('title') as string,
    description: formData.get('description') as string,
  };

  try {
    await createProductCategory(data);
    return { success: true };
  } catch (error) {
    console.error('Error creating product category:', error);
    return { errors: ['Failed to create product category'] };
  }
}
