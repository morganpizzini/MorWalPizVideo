import { VideoCard, type VideoCardProps } from './VideoCard.js';
import { EmptyState } from './EmptyState.js';

export interface VideoCardRailProps {
    title: string;
    items: VideoCardProps[];
    emptyTitle?: string;
    emptyMessage?: string;
}

export function VideoCardRail({ title, items, emptyTitle, emptyMessage }: VideoCardRailProps) {
    return (
        <section className="pb-rail" data-testid="pb-rail">
            <h2 className="pb-rail__title">{title}</h2>
            {items.length === 0 ? (
                <EmptyState
                    title={emptyTitle ?? 'Nothing here yet'}
                    message={emptyMessage ?? 'Check back soon.'}
                />
            ) : (
                <div className="pb-rail__items">
                    {items.map(item => (
                        <VideoCard key={item.youtubeId} {...item} />
                    ))}
                </div>
            )}
        </section>
    );
}

export default VideoCardRail;
