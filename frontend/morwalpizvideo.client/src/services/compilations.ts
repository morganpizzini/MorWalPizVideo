import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { Compilation } from '@morwalpizvideo/models';

export function getCompilationByUrl(url: string): Promise<Compilation> {
    return get(ComposeUrl(endpoints.COMPILATIONS_BY_URL, { url }));
}