import { getPublicNavigation } from '@morwalpizvideo/services';
import type { PublicNavigation } from '@morwalpizvideo/models';

export function fetchPublicNavigation(): Promise<PublicNavigation | null> {
    return getPublicNavigation();
}