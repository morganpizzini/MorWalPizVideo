import { ActionFunctionArgs, data } from 'react-router';
import { post, put, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { CreateCategoryDTO, UpdateCategoryDTO } from '@/models';

export default async function action({ request, params }: ActionFunctionArgs) {
  const values = Object.fromEntries(await request.formData()) as
    | CreateCategoryDTO
    | UpdateCategoryDTO;
  const errors: Record<string, string | string[]> = {};

  if (!values.title || values.title.trim().length === 0) {
    errors.title = 'Title cannot be empty';
  }
  if (!values.description || values.description.trim().length === 0) {
    errors.description = 'Description cannot be empty';
  }
  if (Object.keys(errors).length > 0) {
    return data({ success: false, errors }, { status: 400 });
  }

  try {
    const response = params.id
      ? await put(ComposeUrl(endpoints.CATEGORIES_DETAIL, { categoryId: params.id }), values)
      : await post(endpoints.CATEGORIES, values);

    if (response?.errors) {
      return data(
        { success: false, errors: { generics: response.errors } },
        { status: response.status ?? 500 }
      );
    }

    return data({ success: true }, { status: params.id ? 200 : 201 });
  } catch {
    return data({ success: false, errors: { generics: ['API error found'] } }, { status: 500 });
  }
}
