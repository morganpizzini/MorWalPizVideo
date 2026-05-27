import { getProduct, fetchProductCategories } from '@morwalpizvideo/services';

export default async function loader({ params }: { params: { productId?: string } }) {
  const categoriesPromise = fetchProductCategories();

  if (params.productId) {
    const [product, categories] = await Promise.all([
      getProduct(params.productId),
      categoriesPromise,
    ]);
    return { product, categories, breadcrumbIdentifier: product.title };
  }

  const categories = await categoriesPromise;
  return { product: null, categories };
}
