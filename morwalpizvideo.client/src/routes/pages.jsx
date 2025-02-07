import { useLoaderData } from "react-router"
import SEO from "@utils/seo";
import ReactGA from "react-ga4"
export default function Matches() {
    const { page } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: page.title })
    return (
        <>
            <SEO
                title={page.title}
                description={page.shortContent}
                imageUrl={page.thumbnailUrl}
                type='article' />
            <div id="page-container" className="p-4 bg-white">
                <h1 className="page-title">{page.title}</h1>
                <hr />
                {page.videoId &&
                    <iframe width="100%" height="450px" className="rounded" src={`https://www.youtube.com/embed/${page.videoId}?autoplay=1&mute=1`} title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>    
                }
                <div className="page-text row align-items-center">
                    {page.thumbnailUrl.length > 0 &&
                        <div className={`text-center col-12 ${page.content.length > 0 ? 'col-md-3' : 'col-md-4 offset-md-4'} order-1 order-md-2`} >
                            <img className="img-fluid" alt={page.title} src={page.thumbnailUrl} />
                        </div>
                    }
                    {page.content.length > 0 &&
                        <div className="col-12 col-md-9 order-2 order-md-1 p-3" dangerouslySetInnerHTML={{ __html: page.content }}></div>
                    }
                </div>
            </div>
        </>
    );
}