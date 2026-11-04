import { get, endpoints } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ categories: Category[] }> {
  return { categories: await get(endpoints.CATEGORIES) as Category[] };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
