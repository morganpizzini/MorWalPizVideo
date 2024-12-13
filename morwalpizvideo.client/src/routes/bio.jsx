import { Link, useLoaderData } from "react-router-dom"
import './bio.scss'
import SEO from "@utils/seo";
import { getBioLinks } from "@services/bioLinks";
import TitleComponent from "@layouts/title-header";
import ReactGA from "react-ga4"
export async function loader() {
    const links = await getBioLinks();
    return { links };
}

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
            <TitleComponent dimensions="small"/>

            <div id="page-container" className="p-4 text-center">
                <img className="page-image rounded-circle img-thumbnail" style={{ "height": "75px", "width": "75px" }} alt='chi sono' src={`/images/pages/chi-sono.jpg`} />
                <hr />
                <div className="link-container">
                    {links.map(link => (
                        <>
                            <div className="bg-white">
                                <h4 className="text-uppercase mb-0">
                                    <i className={link.icon}></i>
                                    {link.title}</h4>
                                <p className="mb-0 text-secondary">{link.description}</p>
                                <Link to={link.url} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                            </div>
                        </>
                    ))}
                </div>
            </div>
        </>
    );
}