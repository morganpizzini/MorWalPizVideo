import { useLoaderData } from "react-router-dom"
import './matches.scss'
import SEO from "@utils/seo";
import { getMatch } from "@services/matches";

export async function loader({ params }) {
    const match = await getMatch(params.matchId);
    return { match };
}

export default function Matches() {
    const { match } = useLoaderData();
    return (
        <>
            <SEO
                title={match.title}
                description={match.description}
                imageUrl={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`}
                type='article' />
            <div id="video-container" className="row">
                {match.videos.map(video => (
                    <div key={video.id} className="col-12 mb-3 video-block border rounded border-dark bg-white">
                        <div className="row">
                            <div className="col-md-6">
                                <div className="video-thumbnail">
                                    <iframe width="100%" height="306px" className="rounded" src={`https://www.youtube.com/embed/${video.id}`}  title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>
                                </div>
                            </div>
                            <div className="col-md-6 d-flex align-items-center">
                                <div className="video-details">
                                    <div className="video-title">{video.title}</div>
                                    <div className="video-description">{video.description}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                ))}
            </div>
        </>
    );
}