import { get, endpoints } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';

export interface ImportTarget {
  contentId: string;
  title: string;
  videoCount: number;
}

export default async function loader(): Promise<{ categories: Category[]; targets: ImportTarget[] }> {
  const [categories, targets] = await Promise.all([
    get(endpoints.CATEGORIES) as Promise<Category[]>,
    get('/api/Videos/import-targets') as Promise<ImportTarget[]>,
  ]);
  return { categories, targets };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
