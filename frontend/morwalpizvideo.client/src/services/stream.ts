import { get, frontendEndpoints } from '@morwalpizvideo/services';

export function getConfiguration() {
    return get(frontendEndpoints.CONFIGURATION_STREAM);
}