import { get, frontendEndpoints } from '@morwalpizvideo/services';

export function getProducts() {
    return get(frontendEndpoints.PRODUCTS);
}