import { fetchProducts } from '@morwalpizvideo/services';

export async function loader() {
  const products = await fetchProducts();
  return { products };
}
