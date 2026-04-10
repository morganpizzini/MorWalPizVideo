import { getProductCategory } from '@morwalpizvideo/services';

export async function loader({ params }: { params: { categoryId: string } }) {
  const productCategory = await getProductCategory(params.categoryId);
  return { breadcrumbIdentifier: productCategory.title, productCategory };
}
