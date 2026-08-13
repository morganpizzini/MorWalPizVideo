import { get, endpoints } from '@morwalpizvideo/services';
import type { Category, Channel } from '@morwalpizvideo/models';

export interface ImportTarget {
  contentId: string;
  title: string;
  videoCount: number;
}

export default async function loader(): Promise<{ categories: Category[]; channels: Channel[]; targets: ImportTarget[] }> {
  const [categories, channels, targets] = await Promise.all([
    get(endpoints.CATEGORIES) as Promise<Category[]>,
    get(endpoints.CHANNELS_ACCESSIBLE) as Promise<Channel[]>,
    get('/api/Videos/import-targets') as Promise<ImportTarget[]>,
  ]);
  return { categories, channels, targets };
}

export type LoaderData = Awaited<ReturnType<typeof loader>>;
