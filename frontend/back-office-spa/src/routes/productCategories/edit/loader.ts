import { getProductCategory } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export async function loader({ params }: LoaderFunctionArgs) {
  const productCategory = await getProductCategory(params.categoryId!);
  return { breadcrumbIdentifier: productCategory.title, productCategory };
}
