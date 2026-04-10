import { fetchProductCategories } from '@morwalpizvideo/services';
import type { VideoProductCategory } from '@morwalpizvideo/models';

export default async function loader(): Promise<VideoProductCategory[]> {
  return fetchProductCategories();
}
