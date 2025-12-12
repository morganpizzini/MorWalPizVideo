import { getProductCategory } from '@services/apiService';

export async function loader({ params }: { params: { categoryId: string } }) {
  const productCategory = await getProductCategory(params.categoryId);
  return { breadcrumbIdentifier: productCategory.title, productCategory };
}
