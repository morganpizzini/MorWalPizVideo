import { get, endpoints, ComposeUrl } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs) {
  return get(ComposeUrl(endpoints.SHORTLINKS_DETAIL, { querylinkId: params.id! }));
}
