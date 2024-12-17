import { Link, useLoaderData } from "react-router"
import './bio.scss'
import SEO from "@utils/seo";
import TitleComponent from "@layouts/title-header";
import ReactGA from "react-ga4"

export default function Matches() {
    const { links } = useLoaderData();
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: 'bio' })
    return (
        <>
            <SEO
                title='Bio'
                description='MorWalPiz biografia'
                imageUrl={`/images/pages/chi-sono.jpg`}
                type='article' />
            <div>
                <TitleComponent dimensions="small" />

                <div id="page-container" className="container p-4 text-center">
                    <img className="page-image rounded-circle img-thumbnail" style={{ "height": "75px", "width": "75px" }} alt='chi sono' src={`/images/pages/chi-sono.jpg`} />
                    <hr />
                    <div className="link-container">
                        {links.map(link => (
                            <>
                                <div className="bg-white">
                                    <h4 className="text-uppercase mb-0">
                                        <i className={`me-2 fs-125 ${link.icon}`}></i>
                                        {link.title}</h4>
                                    <p className="mb-0 text-secondary">{link.description}</p>
                                    <Link to={link.url} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                                </div>
                            </>
                        ))}
                    </div>
                </div>
            </div>
        </>
    );
}