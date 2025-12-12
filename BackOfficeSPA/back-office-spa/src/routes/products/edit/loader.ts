import { getProduct, fetchProductCategories } from '@services/apiService';

export async function loader({ params }: { params: { productId: string } }) {
  const [product, categories] = await Promise.all([
    getProduct(params.productId),
    fetchProductCategories()
  ]);
  
    return { product, categories, breadcrumbIdentifier: product.title };
}
