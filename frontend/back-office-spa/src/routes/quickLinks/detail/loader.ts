import { getQuickLinks } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';
import { throwIfQuickLinksApiError } from '../response';

export default function loader({ params }: LoaderFunctionArgs) {
  return getQuickLinks(params.id!).then(response => {
    throwIfQuickLinksApiError(response, 'QuickLinks not found');
    return response;
  });
}
