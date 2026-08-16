import { getPage } from '@morwalpizvideo/services';
import type { PageAdmin } from '@morwalpizvideo/models';
import { throwIfPagesApiError } from '../response';

export default async function loader({ params }: { params: { id?: string } }): Promise<PageAdmin | null> {
  if (!params.id) return null;
  const response = await getPage(params.id);
  throwIfPagesApiError(response, 'Unable to load page');
  return response as PageAdmin;
}