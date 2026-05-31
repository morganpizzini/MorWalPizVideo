import type { MouseEvent } from 'react';
import type { VideoCardChannel } from './VideoCard.js';

export interface CategoryVideoRowProps {
    youtubeId: string;
    title: string;
    thumbnailUrl?: string;
    duration?: string;
    channel: VideoCardChannel;
    description?: string;
    publishedAt?: string;
    onClick?: (youtubeId: string, ev: MouseEvent<HTMLElement>) => void;
}

export function CategoryVideoRow({
    youtubeId,
    title,
    thumbnailUrl,
    duration,
    channel,
    description,
    publishedAt,
    onClick,
}: CategoryVideoRowProps) {
    return (
        <article
            className="pb-row"
            role="button"
            tabIndex={0}
            onClick={ev => onClick?.(youtubeId, ev)}
            onKeyDown={ev => {
                if (ev.key === 'Enter' || ev.key === ' ') {
                    ev.preventDefault();
                    onClick?.(youtubeId, ev as unknown as MouseEvent<HTMLElement>);
                }
            }}
            data-testid="pb-row"
            data-youtube-id={youtubeId}
        >
            <div className="pb-row__thumb">
                {thumbnailUrl ? <img src={thumbnailUrl} alt="" loading="lazy" /> : <div className="pb-card__placeholder">▶</div>}
                {duration ? <span className="pb-card__duration">{duration}</span> : null}
            </div>
            <div className="pb-row__body">
                <div className="pb-row__title">{title}</div>
                <div className="pb-row__channel">{channel.channelName}</div>
                {description ? <div className="pb-row__description">{description}</div> : null}
                {publishedAt ? <div className="pb-row__published">{publishedAt}</div> : null}
            </div>
        </article>
    );
}

export default CategoryVideoRow;
