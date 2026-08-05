import { get, endpoints } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';

export default async function loader(): Promise<{ categories: Category[] }> {
  const categories = await get(endpoints.CATEGORIES) as Category[];
  return { categories };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
