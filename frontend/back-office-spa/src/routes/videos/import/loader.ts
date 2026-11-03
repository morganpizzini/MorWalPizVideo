import { get, endpoints } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';
import { useAppStore } from '../../../state/appStore'; // Importing app store for state management

export default async function loader(): Promise<{ categories: Category[] }> {
  return { categories: await get(endpoints.CATEGORIES) as Promise<Category[]> };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
