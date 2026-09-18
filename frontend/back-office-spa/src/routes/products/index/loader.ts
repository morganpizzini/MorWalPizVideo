import { fetchProducts } from '@morwalpizvideo/services';
import { requireChannelPayload } from '../../channels/response';

export async function loader() {
  const products = requireChannelPayload(await fetchProducts(), 'Unable to load products');
  return { products };
}
