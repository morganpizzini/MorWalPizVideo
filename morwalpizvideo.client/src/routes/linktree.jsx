import { useLoaderData } from "react-router";
import { Helmet } from "react-helmet-async";
import { getCreatorImage } from "@services/linktree";
import "./linktree.scss";

export default function Linktree() {
    const { match, videoLinks } = useLoaderData();

    const handleVideoLinkClick = (videoLink) => {
        // Track click analytics if needed
        console.log(`Clicked on ${videoLink.contentCreatorName}'s video`);
        
        // Open the short link or YouTube video
        if (videoLink.shortLinkCode) {
            window.open(`/sl/${videoLink.shortLinkCode}`, '_blank');
        } else {
            window.open(`https://www.youtube.com/watch?v=${videoLink.youTubeVideoId}`, '_blank');
        }
    };

    const getCreatorInitials = (name) => {
        return name
            .split(' ')
            .map(word => word.charAt(0))
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
                        videoLinks.map((videoLink, index) => (
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
                                        <div className="video-info">
                                            <span className="youtube-icon">📺</span>
                                            Guarda su YouTube
                                        </div>
                                    </div>
                                    
                                    {videoLink.imageName ? (
                                        <img
                                            src={getCreatorImage(videoLink.imageName)}
                                            alt={videoLink.contentCreatorName}
                                            className="creator-image"
                                            onError={(e) => {
                                                // Fallback to placeholder if image fails to load
                                                e.target.style.display = 'none';
                                                e.target.nextSibling.style.display = 'flex';
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
                                
                                {videoLink.shortLinkCode && (
                                    <div className="short-link-info">
                                        <span className="short-link">
                                            morwal.tv/sl/{videoLink.shortLinkCode}
                                        </span>
                                    </div>
                                )}
                            </div>
                        ))
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
