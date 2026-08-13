import { getQuickLinks } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';
import { throwIfQuickLinksApiError } from '../response';

export default async function loader({ params }: LoaderFunctionArgs) {
  if (!params.id) return { quickLinks: null };
  const response = await getQuickLinks(params.id);
  throwIfQuickLinksApiError(response, 'QuickLinks not found');
  return { quickLinks: response };
}
