import type { MouseEvent } from 'react';

export interface VideoCardChannel {
    channelName: string;
    avatarUrl?: string;
}

export interface VideoCardProps {
    youtubeId: string;
    title: string;
    thumbnailUrl?: string;
    duration?: string;
    channel: VideoCardChannel;
    publishedAt?: string;
    onClick?: (youtubeId: string, ev: MouseEvent<HTMLElement>) => void;
}

function channelInitials(name: string): string {
    return name
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map(part => part[0]?.toUpperCase() ?? '')
        .join('') || '?';
}

export function VideoCard({
    youtubeId,
    title,
    thumbnailUrl,
    duration,
    channel,
    publishedAt,
    onClick,
}: VideoCardProps) {
    const handleClick = (ev: MouseEvent<HTMLElement>) => {
        onClick?.(youtubeId, ev);
    };

    return (
        <article
            className="pb-card"
            role="button"
            tabIndex={0}
            onClick={handleClick}
            onKeyDown={ev => {
                if (ev.key === 'Enter' || ev.key === ' ') {
                    ev.preventDefault();
                    onClick?.(youtubeId, ev as unknown as MouseEvent<HTMLElement>);
                }
            }}
            data-testid="pb-card"
            data-youtube-id={youtubeId}
        >
            <div className="pb-card__thumb">
                {thumbnailUrl ? (
                    <img src={thumbnailUrl} alt="" loading="lazy" />
                ) : (
                    <div className="pb-card__placeholder" data-testid="pb-card-thumb-placeholder">
                        <span aria-hidden="true">▶</span>
                    </div>
                )}
                {duration ? (
                    <span className="pb-card__duration" data-testid="pb-card-duration">{duration}</span>
                ) : null}
            </div>
            <div className="pb-card__body">
                <div className="pb-card__avatar" aria-hidden="true">
                    {channel.avatarUrl
                        ? <img src={channel.avatarUrl} alt="" />
                        : <span data-testid="pb-card-avatar-initials">{channelInitials(channel.channelName)}</span>}
                </div>
                <div className="pb-card__meta">
                    <div className="pb-card__title" title={title}>{title}</div>
                    <div className="pb-card__channel">{channel.channelName}</div>
                    {publishedAt ? (
                        <div className="pb-card__published" data-testid="pb-card-published">{publishedAt}</div>
                    ) : null}
                </div>
            </div>
        </article>
    );
}

export default VideoCard;
