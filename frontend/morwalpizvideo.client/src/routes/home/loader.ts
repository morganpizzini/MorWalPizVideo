import { getMatches } from '@services/matches';
import { getConfiguration } from '@services/stream';
import { getPublicSurveys } from '@services/surveys';
import { getSponsors } from '@services/sponsors';
import { getPublicBlog, getPublicChannelNews, requireSuccessfulResponse } from '@morwalpizvideo/services';
import type { ShouldRevalidateFunctionArgs } from 'react-router';

export function homeShouldRevalidate({ currentUrl, nextUrl, defaultShouldRevalidate }: ShouldRevalidateFunctionArgs): boolean {
  if (currentUrl.pathname === nextUrl.pathname && currentUrl.search !== nextUrl.search) return false;
  return defaultShouldRevalidate;
}

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
    const [response, configuration, surveys, channelNews, sponsors, blog] = await Promise.all([
      getMatches(true).then(requireSuccessfulResponse),
      getConfiguration().then(requireSuccessfulResponse),
      getPublicSurveys().then(requireSuccessfulResponse),
      getPublicChannelNews().then(requireSuccessfulResponse),
      getSponsors().then(requireSuccessfulResponse).catch(() => [] as SponsorItem[]),
      getPublicBlog(1).catch(() => null),
    ]);

    return {
      matches: response.data ?? [],
      total: response.count,
      next: response.next,
      configuration: configuration ?? {},
      surveys: surveys ?? [],
      channelNews: channelNews ?? [],
      sponsors: sponsors ?? [],
      articles: blog?.items ?? [],
      articlesUnavailable: blog === null,
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
      articles: [],
      articlesUnavailable: true,
      error: true,
    };
  }
}
