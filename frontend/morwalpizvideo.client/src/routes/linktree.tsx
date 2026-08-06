import { useLoaderData } from "react-router";
import { Helmet } from "react-helmet-async";
import { getCreatorImage } from "@services/linktree";
import "./linktree.scss";

interface VideoLinkItem {
    contentCreatorName: string;
    shortLinkUrl?: string;
    shortLinkCode?: string;
    directVideoUrl?: string;
    youTubeVideoId?: string;
    imageName?: string;
}
interface MatchInfo { title?: string; description?: string }

export default function Linktree() {
    const { match, videoLinks } = useLoaderData() as { match: MatchInfo; videoLinks: VideoLinkItem[] };

    const resolveTargetUrl = (videoLink: VideoLinkItem): string | null => {
        if (videoLink.shortLinkUrl) {
            return videoLink.shortLinkUrl;
        }

        if (videoLink.shortLinkCode) {
            return `/sl/${videoLink.shortLinkCode}`;
        }

        if (videoLink.directVideoUrl) {
            return videoLink.directVideoUrl;
        }

        // Legacy fallback for existing data that only stores YouTubeVideoId.
        if (videoLink.youTubeVideoId) {
            return `https://www.youtube.com/watch?v=${videoLink.youTubeVideoId}`;
        }

        return null;
    };

    const handleVideoLinkClick = (videoLink: VideoLinkItem) => {
        console.log(`Clicked on ${videoLink.contentCreatorName}'s video`);
        const targetUrl = resolveTargetUrl(videoLink);
        if (targetUrl) {
            window.open(targetUrl, '_blank');
        }
    };

    const getCreatorInitials = (name: string) => {
        return name
            .split(' ')
            .map((word: string) => word.charAt(0))
            .join('')
            .toUpperCase()
            .substring(0, 2);
    };

    return (
        <>
            <Helmet>
                <title>{match.title || 'Match'} - YouTube Videos | MorWalPiz</title>
                <meta name="description" content={match.description || `Watch YouTube videos related to ${match.title}`} />
                <meta property="og:title" content={`${match.title} - YouTube Videos`} />
                <meta property="og:description" content={match.description || `Watch YouTube videos related to ${match.title}`} />
                <meta property="og:type" content="website" />
            </Helmet>

            <div className="linktree-container">
                <div className="match-header">
                    <h1 className="match-title">
                        {match.title || 'Match Videos'}
                    </h1>
                    {match.description && (
                        <p className="match-description">
                            {match.description}
                        </p>
                    )}
                </div>

                <div className="video-links-container">
                    {videoLinks.length === 0 ? (
                        <div className="no-links-message">
                            <p>Nessun video disponibile per questo match al momento.</p>
                            <p>Torna più tardi per vedere i contenuti dei creator!</p>
                        </div>
                    ) : (
                        videoLinks.map((videoLink: VideoLinkItem, index: number) => {
                            const targetUrl = resolveTargetUrl(videoLink);

                            return (
                            <div
                                key={index}
                                className="video-link"
                                onClick={() => handleVideoLinkClick(videoLink)}
                                role="button"
                                tabIndex={0}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' || e.key === ' ') {
                                        e.preventDefault();
                                        handleVideoLinkClick(videoLink);
                                    }
                                }}
                            >
                                <div className="creator-info">
                                    <div className="creator-details">
                                        <div className="creator-name">
                                            {videoLink.contentCreatorName}
                                        </div>
                                        <div className="video-info">Apri contenuto</div>
                                    </div>
                                    
                                    {videoLink.imageName ? (
                                        <img
                                            src={getCreatorImage(videoLink.imageName)}
                                            alt={videoLink.contentCreatorName}
                                            className="creator-image"
                                            onError={(e: React.SyntheticEvent<HTMLImageElement>) => {
                                                // Fallback to placeholder if image fails to load
                                                const img = e.currentTarget;
                                                img.style.display = 'none';
                                                const next = img.nextSibling as HTMLElement | null;
                                                if (next) next.style.display = 'flex';
                                            }}
                                        />
                                    ) : null}
                                    
                                    <div 
                                        className="creator-image-placeholder"
                                        style={{
                                            display: videoLink.imageName ? 'none' : 'flex'
                                        }}
                                    >
                                        {getCreatorInitials(videoLink.contentCreatorName)}
                                    </div>
                                </div>
                                
                                {targetUrl && (
                                    <div className="short-link-info">
                                        <span className="short-link">
                                            {videoLink.shortLinkUrl || (videoLink.shortLinkCode ? `morwal.tv/sl/${videoLink.shortLinkCode}` : targetUrl)}
                                        </span>
                                    </div>
                                )}
                            </div>
                        )})
                    )}
                </div>
                
                {videoLinks.length > 0 && (
                    <div className="mt-4 text-center">
                        <small className="text-muted">
                            Clicca su un link per guardare il video
                        </small>
                    </div>
                )}
            </div>
        </>
    );
}
