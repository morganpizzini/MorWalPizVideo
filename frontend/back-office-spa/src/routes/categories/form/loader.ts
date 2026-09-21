import type { LoaderFunctionArgs } from 'react-router';
import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';

export default async function loader({ params }: LoaderFunctionArgs): Promise<Category | null> {
  if (!params.id) return null;
  return get(ComposeUrl(endpoints.CATEGORIES_DETAIL, { categoryId: params.id }));
}
