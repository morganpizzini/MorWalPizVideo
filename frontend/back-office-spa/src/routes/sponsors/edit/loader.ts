import { getSponsor } from '@morwalpizvideo/services';
import type { LoaderFunctionArgs } from 'react-router';

export async function loader({ params }: LoaderFunctionArgs) {
  const sponsor = await getSponsor(params.id!);
  return sponsor;
}
