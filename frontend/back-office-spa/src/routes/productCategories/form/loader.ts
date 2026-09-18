import { getProductCategory } from '@morwalpizvideo/services';
import { requireChannelPayload } from '../../channels/response';

export default async function loader({ params }: { params: { categoryId?: string } }) {
  if (params.categoryId) {
    const productCategory = requireChannelPayload(
      await getProductCategory(params.categoryId),
      'Unable to load product category'
    );
    return { productCategory, breadcrumbIdentifier: productCategory.title };
  }
  return { productCategory: null };
}
