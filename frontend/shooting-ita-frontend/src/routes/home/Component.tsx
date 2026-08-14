import { useLoaderData, useNavigate } from 'react-router-dom';
import { HeroCarousel, VideoCardRail, type HeroSlide, type VideoCardProps } from '@morwalpiz/layout';
import { formatRelativeTime } from '@morwalpiz/layout';
import type { MatchWithChannel } from '../../services/shootingItaVideoService';
import type { ChannelNews } from '@morwalpizvideo/models';
import ChannelNewsCarousel from './ChannelNewsCarousel';

interface HomeLoaderData {
    featured: MatchWithChannel[];
    exclusiveRail: MatchWithChannel[];
    channelNews: ChannelNews[];
}

function firstYoutubeId(m: MatchWithChannel): string {
    return m.videoRefs?.[0]?.youtubeId ?? '';
}

function pickPublishedAt(m: MatchWithChannel): string | undefined {
    const candidates: Array<string | undefined> = [];
    for (const r of m.videoRefs ?? []) candidates.push((r as { publishedAt?: string }).publishedAt);
    for (const v of m.videos ?? []) candidates.push(v.publishedAt);
    return candidates.find(Boolean);
}

function toSlide(navigateTo: (id: string) => void) {
    return (m: MatchWithChannel): HeroSlide => ({
        youtubeId: firstYoutubeId(m),
        title: (m as { title?: string }).title ?? '',
        channelName: m.owner.channelName,
        artworkUrl: (m as { thumbnailVideoId?: string }).thumbnailVideoId
            ? `https://img.youtube.com/vi/${(m as { thumbnailVideoId?: string }).thumbnailVideoId}/maxresdefault.jpg`
            : undefined,
        onPlay: () => navigateTo(firstYoutubeId(m)),
    });
}

function toCard(navigateTo: (id: string) => void) {
    return (m: MatchWithChannel): VideoCardProps => {
        const id = firstYoutubeId(m);
        const publishedAt = pickPublishedAt(m);
        return {
            youtubeId: id,
            title: (m as { title?: string }).title ?? '',
            thumbnailUrl: id ? `https://img.youtube.com/vi/${id}/hqdefault.jpg` : undefined,
            channel: { channelName: m.owner.channelName, avatarUrl: m.owner.avatarUrl },
            publishedAt: publishedAt ? formatRelativeTime(publishedAt) : undefined,
            onClick: () => navigateTo(id),
        };
    };
}

export default function HomeRoute() {
    const data = useLoaderData() as HomeLoaderData;
    const navigate = useNavigate();
    const goVideo = (id: string) => navigate(`/video/${id}`);

    return (
        <>
            <ChannelNewsCarousel items={data.channelNews} />
            <HeroCarousel slides={data.featured.map(toSlide(goVideo))} />
            <VideoCardRail
                title="Exclusives"
                items={data.exclusiveRail.map(toCard(goVideo))}
                emptyTitle="No exclusives yet"
                emptyMessage="Pin a category in your environment to see exclusives here."
            />
        </>
    );
}
