import { fetchProducts, fetchProductCategories } from '@morwalpizvideo/services';
import { requireChannelPayload } from '../../channels/response';

export async function loader() {
  const [productsResponse, categoriesResponse] = await Promise.all([
    fetchProducts(),
    fetchProductCategories(),
  ]);
  return {
    products: requireChannelPayload(productsResponse, 'Unable to load products'),
    categories: requireChannelPayload(categoriesResponse, 'Unable to load product categories'),
  };
}
