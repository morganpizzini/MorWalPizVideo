import { useLoaderData } from "react-router"
import './matches.scss'
import SEO from "@utils/seo";
import DateDisplay from "@utils/date-display";
import ReactGA from "react-ga4"

export default function Matches() {
    const { match } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: match.title })
    return (
        <>
            <SEO
                title={match.title}
                description={match.description}
                imageUrl={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`}
                type='article' />
            <div id="video-container" className="row">
                {match.videos.map(video => (
                    <div key={video.youtubeId} className="col-12 mb-3 video-block border rounded border-dark bg-white">
                        <div className="row">
                            <div className="col-md-6">
                                <div className="video-thumbnail">
                                    <iframe width="100%" height="306px" className="rounded"
                                        src={`https://www.youtube.com/embed/${video.youtubeId}`} title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>
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
                ))}
            </div>
        </>
    );
}