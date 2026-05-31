import { useState } from "react";
import { useLoaderData } from "react-router"
import './matches.scss'
import SEO from "@utils/seo";
import DateDisplay from "@utils/date-display";
import ReactGA from "react-ga4"
import Gallery from "@utils/gallery";
import { VideoPlayer } from "@morwalpiz/layout";
interface MatchVideoRef { youtubeId: string; title: string; description?: string; category?: string; publishedAt: string }
interface MatchData { title: string; description?: string; thumbnailUrl: string; videoRefs: MatchVideoRef[] }
interface MatchImage { source: string; title: string; description: string }

export default function Matches() {
    const { match, images } = useLoaderData() as { match: MatchData; images: MatchImage[] };
    if (typeof window !== 'undefined') {
        ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: match.title })
    }
    return (
        <>
            <SEO
                title={match.title}
                description={match.description ?? ''}
                imageUrl={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`}
                type='article' />
            {images.length > 0 && 
                <GalleryComponent className="mb-2" images={images} />
            }
            <div id="video-container" className="row">
                {match.videoRefs.map((video: MatchVideoRef) => (
                    <div key={video.youtubeId} className="col-12 mb-3">
                        <div key={video.youtubeId} className="video-block border rounded border-dark bg-white">
                            <div className="row">
                                <div className="col-md-6">
                                    <div className="video-thumbnail">
                                        <VideoPlayer youtubeId={video.youtubeId} height="306px" />
                                    </div>
                                </div>
                                <div className="col-md-6 d-flex align-items-center">
                                    <div className="video-details">
                                        <p className="text-muted">{video.category}</p>
                                        <div className="video-title">{video.title}</div>
                                        <div className="video-description">{video.description}</div>
                                        <DateDisplay className="text-muted text-end" dateString={video.publishedAt} />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                ))}
            </div>
        </>
    );
}

function GalleryComponent({ className, images }: { className?: string; images: MatchImage[] }) {
    const [showGallery, setShowGallery] = useState(false);

    return (<div className={className}>
        <div className="alert alert-secondary my-3 text-center fw-bold pop-up text-uppercase c-pointer" role="alert" onClick={() => {
            setShowGallery(!showGallery)
        }}>
            {showGallery ? "Nascondi galleria" : "Mostra galleria"}
        </div>
        {showGallery &&
            <Gallery images={images} />
        }
    </div>
    )
}