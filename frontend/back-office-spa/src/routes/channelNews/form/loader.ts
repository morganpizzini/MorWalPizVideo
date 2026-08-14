import { getChannelNews } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  if (!params.id) return null;
  return getChannelNews(params.id);
}
