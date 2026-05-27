import { ActionFunctionArgs, data } from 'react-router';
import { createProduct, updateProduct } from '@morwalpizvideo/services';
import type { CreateProductDTO, UpdateProductDTO } from '@morwalpizvideo/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const formData = await request.formData();

  const title = formData.get('title') as string;
  const description = formData.get('description') as string;
  const url = formData.get('url') as string;
  const categoryIds = JSON.parse((formData.get('categoryIds') as string) || '[]');

  const errors: Record<string, string> = {};
  if (!title?.trim()) errors['title'] = 'Title is required';
  if (!description?.trim()) errors['description'] = 'Description is required';
  if (!url?.trim()) errors['url'] = 'URL is required';

  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    if (params.productId) {
      const productData: UpdateProductDTO = { title, description, url, categoryIds };
      await updateProduct(params.productId, productData);
    } else {
      const productData: CreateProductDTO = { title, description, url, categoryIds };
      await createProduct(productData);
    }
    return data({ success: true }, { status: params.productId ? 200 : 201 });
  } catch (error: any) {
    return data(
      { success: false, errors: { generics: [error.message || 'An unexpected error occurred'] } },
      { status: 500 }
    );
  }
}
