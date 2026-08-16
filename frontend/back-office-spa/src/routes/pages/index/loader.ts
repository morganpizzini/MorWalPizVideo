import { fetchPages } from '@morwalpizvideo/services';
import type { PageAdmin } from '@morwalpizvideo/models';
import { throwIfPagesApiError } from '../response';

export default async function loader(): Promise<PageAdmin[]> {
  const response = await fetchPages();
  throwIfPagesApiError(response, 'Unable to load pages');
  return response as PageAdmin[];
}