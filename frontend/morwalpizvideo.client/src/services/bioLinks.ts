import { get, frontendEndpoints } from '@morwalpizvideo/services';

export function getBioLinks() {
    return get(frontendEndpoints.BIOLINKS);
}