import { fetchProducts } from '@services/apiService';

export async function loader() {
  const products = await fetchProducts();
  return { products };
}
