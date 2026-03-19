import { useLoaderData } from "react-router"
import './matches.scss'
import SEO from "@utils/seo";
import ReactGA from "react-ga4"
import TitleComponent from "@layouts/title-header";


export default function Compilations() {
    const { compilation } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: compilation.title })

    return (
        <>
            <SEO
                title={compilation.title}
                description={compilation.description}
                imageUrl={compilation.videos?.length > 0 ? `https://img.youtube.com/vi/${compilation.videos[0].youtubeId}/hqdefault.jpg` : ''}
                type='article' />
            <div className="container">
                <TitleComponent hideLink={true} dimensions="small" />

                <h1 className="text-center mb-2">{compilation.title}</h1>
                {compilation.description && (
                    <p className="text-center mb-2">{compilation.description}</p>
                )}
                <hr />

                <div id="video-container" className="row">
                    {compilation.videos?.map(video => (
                        <div key={video.youtubeId} className="col-6 mb-3">
                            <div className="video-block border rounded border-dark bg-white">
                                <div className="video-thumbnail">
                                    <iframe
                                        width="100%"
                                        height="180px"
                                        className="rounded"
                                        src={`https://www.youtube.com/embed/${video.youtubeId}`}
                                        title="YouTube video player"
                                        frameBorder="0"
                                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                                        referrerPolicy="strict-origin-when-cross-origin"
                                        allowFullScreen>
                                    </iframe>
                                </div>
                                <div className="video-details">
                                    <div className="video-title">{video.title}</div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </>
    );
}
