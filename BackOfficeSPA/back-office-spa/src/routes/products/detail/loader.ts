import { getProduct } from '@services/apiService';

export async function loader({ params }: { params: { productId: string } }) {
    const product = await getProduct(params.productId);
    return { product, breadcrumbIdentifier: product.title };
}
