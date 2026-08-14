import { getPublicChannelNewsByIdOrSlug } from '@morwalpizvideo/services';
import type { LoaderFunction } from 'react-router-dom';

export const channelNewsLoader: LoaderFunction = async ({ params }) => {
    if (!params.idOrSlug) throw new Response('ChannelNews not found', { status: 404 });
    return getPublicChannelNewsByIdOrSlug(params.idOrSlug);
};