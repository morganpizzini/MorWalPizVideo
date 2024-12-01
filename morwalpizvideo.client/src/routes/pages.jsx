import { useLoaderData } from "react-router-dom"
import './pages.scss'
import SEO from "@utils/seo";
import { getPages } from "@services/pages";
import ReactGA from "react-ga4"
export async function loader({ params }) {
    const page = await getPages(params.pageId);
    return { page };
}

export default function Matches() {
    const { page } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: page.title })
    return (
        <>
            <SEO
                title={page.title}
                description={page.shortContent}
                imageUrl={`/images/pages/${page.thumbnailUrl}`}
                type='article' />
            <div id="page-container" className="p-4 bg-white">
                <h1 className="page-title">{page.title}</h1>
                <hr />
                <div className="page-text">
                    <div className="page-content" dangerouslySetInnerHTML={{ __html: page.content }}></div>
                    { page.thumbnailUrl.length > 0 && 
                        <img className="page-image" alt={page.title} src={`${location.protocol + '//' + location.host}/images/pages/${page.thumbnailUrl}`} />
                    }
                </div>
            </div>
        </>
    );
}