import type { LoaderFunctionArgs } from 'react-router';
import { getMatch, getMatchImages } from '@services/matches';
import type { Match, MatchImage } from '@morwalpizvideo/models';

export default async function loader({ params }: LoaderFunctionArgs) {
  const match = (await getMatch(params.matchId as string)) as Match;
  const images = match.images ?? ((await getMatchImages(params.matchId as string)) as string[]);
  return {
    match,
    images: (images as (MatchImage | string)[]).map((image, index) =>
      typeof image === 'string'
        ? { source: image, title: `Match image ${index + 1}` }
        : { source: image.publicUrl, title: image.altText || `Match image ${index + 1}` }
    ),
  };
}
