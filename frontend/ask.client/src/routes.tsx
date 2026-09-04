import { Helmet } from 'react-helmet-async';
import { useLoaderData, type LoaderFunctionArgs, type RouteObject } from 'react-router';
import { getAskCampaign, type AskPublicCampaign } from '@morwalpizvideo/services';
import AskPage from './AskPage';

export async function loader({ params }: LoaderFunctionArgs): Promise<{ campaign: AskPublicCampaign }> {
  if (!params.channelName || !params.campaignSlug) throw new Response('Not found', { status: 404 });
  const campaign = await getAskCampaign(params.channelName, params.campaignSlug);
  return { campaign };
}

export function createAskMetadata(campaign: AskPublicCampaign, origin: string) {
  return {
    title: `${campaign.title} | Ask`,
    description: campaign.description,
    canonical: `${origin}/${encodeURIComponent(campaign.channelName)}/${encodeURIComponent(campaign.slug)}`,
  };
}

function Metadata() {
  const { campaign } = useLoaderData() as { campaign: AskPublicCampaign };
  const metadata = createAskMetadata(campaign, typeof window === 'undefined' ? 'https://ask.morwalpiz.com' : window.location.origin);
  return <Helmet>
    <title>{metadata.title}</title>
    <meta name="description" content={metadata.description} />
    <meta property="og:title" content={campaign.title} />
    <meta property="og:description" content={metadata.description} />
    <meta property="og:type" content="website" />
    <link rel="canonical" href={metadata.canonical} />
  </Helmet>;
}

export function AskRoute() { return <><Metadata /><AskPage /></>; }
export const routes: RouteObject[] = [{ path: '/:channelName/:campaignSlug', loader, Component: AskRoute }];
