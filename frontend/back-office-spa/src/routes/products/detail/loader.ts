import { getProduct } from '@morwalpizvideo/services';
import type { Product } from '@morwalpizvideo/models';
import type { LoaderFunctionArgs } from 'react-router';
import { requireChannelPayload } from '../../channels/response';

export async function loader({ params }: LoaderFunctionArgs) {
  const product = requireChannelPayload(
    await getProduct(params.productId!),
    'Unable to load product'
  ) as Product;
  return { product, breadcrumbIdentifier: product.title };
}
