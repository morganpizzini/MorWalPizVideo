import { useLoaderData, useNavigate } from 'react-router-dom';
import { CategoryBanner, CategoryVideoRow, EmptyState, formatRelativeTime } from '@morwalpiz/layout';
import type { MatchWithChannel } from '../../services/shootingItaVideoService';

interface Data { items: MatchWithChannel[] }

export default function ExclusivesRoute() {
    const { items } = useLoaderData() as Data;
    const navigate = useNavigate();
    return (
        <>
            <CategoryBanner title="Exclusives" />
            {items.length === 0 ? (
                <EmptyState title="No exclusives yet" message="Pin a category in your environment to see exclusives here." />
            ) : items.map(m => {
                const id = m.videoRefs?.[0]?.youtubeId ?? '';
                const publishedAt = m.videos?.[0]?.publishedAt ?? (m.videoRefs?.[0] as { publishedAt?: string })?.publishedAt;
                return (
                    <CategoryVideoRow
                        key={(m as { matchId?: string }).matchId ?? id}
                        youtubeId={id}
                        title={(m as { title?: string }).title ?? ''}
                        thumbnailUrl={id ? `https://img.youtube.com/vi/${id}/hqdefault.jpg` : undefined}
                        channel={{ channelName: m.owner.channelName, avatarUrl: m.owner.avatarUrl }}
                        description={(m as { description?: string }).description}
                        publishedAt={publishedAt ? formatRelativeTime(publishedAt) : undefined}
                        onClick={() => navigate(`/video/${id}`)}
                    />
                );
            })}
        </>
    );
}
