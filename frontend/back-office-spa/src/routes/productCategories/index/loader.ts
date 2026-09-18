import { fetchProductCategories } from '@morwalpizvideo/services';
import type { VideoProductCategory } from '@morwalpizvideo/models';
import { requireChannelPayload } from '../../channels/response';

export default async function loader(): Promise<VideoProductCategory[]> {
  return requireChannelPayload(await fetchProductCategories(), 'Unable to load product categories');
}
