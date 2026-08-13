import { fetchQuickLinks } from '@morwalpizvideo/services';
import type { QuickLinks } from '@morwalpizvideo/models';
import { throwIfQuickLinksApiError } from '../response';

export default function loader(): Promise<QuickLinks[]> {
  return fetchQuickLinks().then(response => {
    throwIfQuickLinksApiError(response, 'Unable to load QuickLinks');
    return response as QuickLinks[];
  });
}
