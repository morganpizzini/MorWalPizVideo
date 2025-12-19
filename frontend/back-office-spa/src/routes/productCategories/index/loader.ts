import { fetchProductCategories } from '@services/apiService';
import type { ProductCategory } from '@morwalpizvideo/models';

export default async function loader(): Promise<ProductCategory[]> {
  return fetchProductCategories();
}
