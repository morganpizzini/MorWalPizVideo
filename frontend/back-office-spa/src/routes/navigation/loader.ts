import { fetchPages, getNavigation } from '@morwalpizvideo/services';
import { throwIfPagesApiError } from '../pages/response';

export default async function loader() {
  const [navigation, pages] = await Promise.all([getNavigation(), fetchPages()]);
  throwIfPagesApiError(navigation, 'Unable to load navigation');
  throwIfPagesApiError(pages, 'Unable to load pages');
  return { navigation, pages };
}