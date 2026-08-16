import { get, endpoints } from '@morwalpizvideo/services';
import type { Category } from '@morwalpizvideo/models';
import { useAppStore } from '../../../state/appStore';

export interface ImportTarget {
  contentId: string;
  title: string;
  videoCount: number;
}

export default async function loader(): Promise<{ categories: Category[]; targets: ImportTarget[] }> {
  const bulkImportEnabled = useAppStore.getState().featureFlags.videoBulkImportEnabled;

  const [categories, targets] = await Promise.all([
    get(endpoints.CATEGORIES) as Promise<Category[]>,
    bulkImportEnabled
      ? get('/api/Videos/import-targets') as Promise<ImportTarget[]>
      : Promise.resolve([]),
  ]);
  return { categories, targets };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
