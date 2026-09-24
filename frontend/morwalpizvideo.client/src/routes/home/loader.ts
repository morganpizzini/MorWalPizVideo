import { getMatches } from '@services/matches';
import { getConfiguration } from '@services/stream';
import { getPublicSurveys } from '@services/surveys';
import { getSponsors } from '@services/sponsors';
import { getPublicChannelNews } from '@morwalpizvideo/services';

interface SponsorItem {
  id: string;
  title: string;
  imgSrc: string;
  url: string;
  channelId?: string;
  shortLinkId?: string;
}

export default async function loader() {
  try {
    const [response, configuration, surveys, channelNews, sponsors] = await Promise.all([
      getMatches(true),
      getConfiguration(),
      getPublicSurveys(),
      getPublicChannelNews(),
      getSponsors().catch(() => [] as SponsorItem[]),
    ]);

    return {
      matches: response.data ?? [],
      total: response.count,
      next: response.next,
      configuration: configuration ?? {},
      surveys: surveys ?? [],
      channelNews: channelNews ?? [],
      sponsors: sponsors ?? [],
      error: false,
    };
  } catch {
    return {
      matches: [],
      total: 0,
      next: undefined,
      configuration: {},
      surveys: [],
      channelNews: [],
      sponsors: [],
      error: true,
    };
  }
}
