import type { ChannelNews } from "@morwalpizvideo/models";
import { publicGet as get } from "./apiTransport";
import frontendEndpoints from "./endpoints-frontend";

export function getPublicChannelNews(): Promise<ChannelNews[]> {
  return get(frontendEndpoints.SHIT_CHANNEL_NEWS) as Promise<ChannelNews[]>;
}

export function getPublicChannelNewsByIdOrSlug(
  idOrSlug: string,
): Promise<ChannelNews> {
  return get(
    frontendEndpoints.SHIT_CHANNEL_NEWS_DETAIL.replace(
      "{idOrSlug}",
      encodeURIComponent(idOrSlug),
    ),
  ) as Promise<ChannelNews>;
}
