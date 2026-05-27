import { ActionFunctionArgs, data } from 'react-router';
import { createProductCategory, updateProductCategory } from '@morwalpizvideo/services';
import type { CreateProductCategoryDTO, UpdateProductCategoryDTO } from '@morwalpizvideo/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();

  const title = formData.get('title') as string;
  const description = formData.get('description') as string;

  const errors: Record<string, string> = {};
  if (!title?.trim()) errors['title'] = 'Title is required';
  if (!description?.trim()) errors['description'] = 'Description is required';

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    if (params.categoryId) {
      const updateData: UpdateProductCategoryDTO = { title, description };
      await updateProductCategory(params.categoryId, updateData);
    } else {
      const createData: CreateProductCategoryDTO = { title, description };
      await createProductCategory(createData);
    }
    return data({ success: true }, { status: params.categoryId ? 200 : 201 });
  } catch (error) {
    return data(
      { success: false, errors: { generics: ['Failed to save product category'] } },
      { status: 500 }
    );
  }
}
