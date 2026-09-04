import type { LoaderFunctionArgs } from 'react-router';
import { getAskCampaign } from '@morwalpizvideo/services';

export default async function askLoader({ params }: LoaderFunctionArgs) {
  if (!params.channelName || !params.campaignSlug) throw new Response('Not found', { status: 404 });
  return { campaign: await getAskCampaign(params.channelName, params.campaignSlug) };
}