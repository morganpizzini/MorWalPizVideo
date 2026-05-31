export interface VideoPlayerProps {
    youtubeId: string;
    autoplay?: boolean;
    muted?: boolean;
    title?: string;
    className?: string;
    width?: string | number;
    height?: string | number;
}

export function VideoPlayer({
    youtubeId,
    autoplay = false,
    muted = false,
    title = 'YouTube video player',
    className = 'rounded',
    width = '100%',
    height = '450px',
}: VideoPlayerProps) {
    const params: string[] = [];
    if (autoplay) params.push('autoplay=1');
    if (muted) params.push('mute=1');
    const qs = params.length ? `?${params.join('&')}` : '';

    return (
        <iframe
            width={width}
            height={height}
            className={className}
            src={`https://www.youtube.com/embed/${youtubeId}${qs}`}
            title={title}
            frameBorder="0"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
            referrerPolicy="strict-origin-when-cross-origin"
            allowFullScreen
            data-testid="pb-video-player"
        />
    );
}

export default VideoPlayer;
