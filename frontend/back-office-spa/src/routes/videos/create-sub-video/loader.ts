import { get, endpoints } from '@morwalpizvideo/services';

export interface LoaderData {
  categories: any[];
  videos: any[];
}

export default async function loader(): Promise<LoaderData> {
  const [categories, videos] = await Promise.all([
    get(endpoints.CATEGORIES),
    get(endpoints.VIDEOS)
  ]);

  return { categories, videos };
}
