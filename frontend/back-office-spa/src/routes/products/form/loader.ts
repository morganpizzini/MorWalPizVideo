import { getProduct, fetchProductCategories } from '@morwalpizvideo/services';
import { requireChannelPayload } from '../../channels/response';

export default async function loader({ params }: { params: { productId?: string } }) {
  const categoriesPromise = fetchProductCategories().then(response =>
    requireChannelPayload(response, 'Unable to load product categories')
  );

  if (params.productId) {
    const [product, categories] = await Promise.all([
      getProduct(params.productId).then(response =>
        requireChannelPayload(response, 'Unable to load product')
      ),
      categoriesPromise,
    ]);
    return { product, categories, breadcrumbIdentifier: product.title };
  }

  const categories = await categoriesPromise;
  return { product: null, categories };
}
