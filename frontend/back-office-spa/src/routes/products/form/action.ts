import { ActionFunctionArgs, data } from 'react-router';
import { createProduct, updateProduct } from '@morwalpizvideo/services';
import type { CreateProductDTO, UpdateProductDTO } from '@morwalpizvideo/models';
import { channelActionError, getChannelApiError } from '../../channels/response';

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
      const response = await updateProduct(params.productId, productData);
      if (getChannelApiError(response))
        return channelActionError(response, 'Unable to update product');
    } else {
      const productData: CreateProductDTO = { title, description, url, categoryIds };
      const response = await createProduct(productData);
      if (getChannelApiError(response))
        return channelActionError(response, 'Unable to create product');
    }
    return data({ success: true }, { status: params.productId ? 200 : 201 });
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'An unexpected error occurred';
    return data({ success: false, errors: { generics: [message] } }, { status: 500 });
  }
}
