import { getSponsor } from '@morwalpizvideo/services';
import type { Sponsor } from '@morwalpizvideo/models';
import type { LoaderFunctionArgs } from 'react-router';

export default async function loader({ params }: LoaderFunctionArgs): Promise<Sponsor | null> {
  return params.sponsorId ? getSponsor(params.sponsorId) : null;
}
