import { getProductCategory } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { categoryId?: string } }) {
  if (params.categoryId) {
    const productCategory = await getProductCategory(params.categoryId);
    return { productCategory, breadcrumbIdentifier: productCategory.title };
  }
  return { productCategory: null };
}
