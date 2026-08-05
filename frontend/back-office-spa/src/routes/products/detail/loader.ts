import { getProduct } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export async function loader({ params }: LoaderFunctionArgs) {
    const product = await getProduct(params.productId!);
    return { product, breadcrumbIdentifier: product.title };
}
