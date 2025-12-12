import { updateProductCategory } from '@services/apiService';
import type { UpdateProductCategoryDTO } from '@models';

export default async function action({ request, params }: { request: Request; params: { categoryId: string } }) {
  const formData = await request.formData();

  const data: UpdateProductCategoryDTO = {
    title: formData.get('title') as string,
    description: formData.get('description') as string,
  };

  try {
    await updateProductCategory(params.categoryId, data);
    return { success: true };
  } catch (error) {
    console.error('Error updating product category:', error);
    return { errors: ['Failed to update product category'] };
  }
}
