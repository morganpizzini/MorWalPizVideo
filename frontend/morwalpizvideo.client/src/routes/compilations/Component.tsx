import { useLoaderData } from "react-router"
import '../matches/style.scss'
import SEO from "@utils/seo";
import ReactGA from "react-ga4"
import TitleComponent from "@layouts/title-header";
import { VideoPlayer } from "@morwalpiz/layout";


interface CompVideo { youtubeId: string; title: string }
interface Compilation { title: string; description?: string; videos?: CompVideo[] }

export default function Compilations() {
    const { compilation } = useLoaderData() as { compilation: Compilation };
    if (typeof window !== 'undefined') {
        ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: compilation.title })
    }

    return (
        <>
            <SEO
                title={compilation.title}
                description={compilation.description ?? ''}
                imageUrl={compilation.videos && compilation.videos.length > 0 ? `https://img.youtube.com/vi/${compilation.videos[0].youtubeId}/hqdefault.jpg` : ''}
                type='article' />
            <div className="container">
                <TitleComponent hideLink={true} dimensions="small" />

                <h1 className="text-center mb-2">{compilation.title}</h1>
                {compilation.description && (
                    <p className="text-center mb-2">{compilation.description}</p>
                )}
                <hr />

                <div id="video-container" className="row">
                    {compilation.videos?.map((video: CompVideo) => (
                        <div key={video.youtubeId} className="col-6 mb-3">
                            <div className="video-block border rounded border-dark bg-white">
                                <div className="video-thumbnail">
                                    <VideoPlayer youtubeId={video.youtubeId} height="180px" />
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
