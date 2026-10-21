import type { LoaderFunctionArgs } from 'react-router';
import { fetchPartners, getCampaign } from '@morwalpizvideo/services';

export default async function loader({ params }: LoaderFunctionArgs) {
  const [campaign, partners] = await Promise.all([
    params.id ? getCampaign(params.id) : null,
    fetchPartners(),
  ]);
  return { campaign, partners };
}
