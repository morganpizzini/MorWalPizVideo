import { Link, useLoaderData } from "react-router-dom";
import { getMatches } from "@services/matches";
import React from "react";
import DateDisplay from "@utils/date-display";
import SEO from "@utils/seo";
import './index.scss'
import { FacebookShareButton, FacebookIcon, WhatsappShareButton, WhatsappIcon } from "react-share";
import ReactGA from "react-ga4"
export async function loader() {
    const matches = await getMatches();
    return { matches };
}
export default function Index() {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title:"Home" })
    const { matches } = useLoaderData();

    let firstMatchId = matches[0];
    if (firstMatchId.videos?.length > 0) {
        firstMatchId = firstMatchId.videos[firstMatchId.videos.length - 1].id;
    } else {
        firstMatchId = firstMatchId.url
    }
    return (
        <>
            <SEO
                title={"MorWalPiz"}
                description={"MorWalPiz"}
                imageUrl={`https://img.youtube.com/vi/${matches[0].thumbnailUrl}/hqdefault.jpg`}
                type='website' />
            <div className="row align-items-center">
                <div className="d-none d-md-block col-md-3">
                    {RenderMatchCard(matches[0], -1)}
                </div>
                <div className="col-12 col-md-9">
                    <iframe width="100%" height="450px" className="rounded" src={`https://www.youtube.com/embed/${firstMatchId}?autoplay=1&mute=1`} title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>
                </div>
            </div>
            <div className="card-columns">
                {matches.map((match, i) => (
                    <React.Fragment key={i}>
                        {RenderMatchCard(match, i)}
                        {i === 7 &&
                            <Banner />
                        }
                        {i === 13 &&
                            <Sponsors />
                        }
                    </React.Fragment>
                ))}
            </div>
            {matches.length < 8 && <Banner />}
            {matches.length < 14 && <Sponsors />}
        </>
    );
}
function Banner() {
    return (
        <Link to={`/attrezzatura`} className="text-decoration-none text-black d-block" style={{ "columnSpan": "all" }}>
            <div className="alert alert-secondary my-3 text-center fw-bold pop-up" role="alert">
                Tutta la mia attrezzatura <i className="fa fa-arrow-right"></i>
            </div>
        </Link>
    )
}

function Sponsors() {
    return (
        <Link to={`/sponsors`} className="text-decoration-none text-black d-block" style={{ "columnSpan": "all" }}>
            <div className="alert alert-secondary my-3 text-center fw-bold pop-up" role="alert">
                I miei sponsors <i className="fa fa-arrow-right"></i>
            </div>
        </Link>
    )
}
function RenderMatchCard(match, i) {
    const className = i == 0 ? "card position-relative d-md-none" : "card position-relative";
    return (
        <div className={className}>
            <img src={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`} className="card-img-top" alt="Video Thumbnail" />
            <div className="card-body">
                {match.isLink &&
                    <p className="text-muted mb-1">{match.category}</p>
                }
                {!match.isLink &&
                    <p className="text-muted mb-1">{match.videos.length} video</p>
                }
                <h5 className="card-title">{match.title}</h5>
                <p className="card-text">{match.description}</p>
                <div className="d-flex justify-content-between align-items-center">
                    {match.isLink &&
                        <div className="d-flex justify-content-start align-items-center">
                            <Link to={`https://youtu.be/${match.url}`} target="_blank" rel="noopener noreferrer" className="me-1 pt-1">
                                <i className="fa-1_8x text-danger fab fa-youtube"></i>
                            </Link>
                            <FacebookShareButton url={`https://youtu.be/${match.url}`} className="Demo__some-network__share-button me-1">
                                <FacebookIcon size={26} round />
                            </FacebookShareButton>
                            <WhatsappShareButton
                                url={`https://youtu.be/${match.url}`}
                                title={match.title}
                                separator=":: "
                                className="Demo__some-network__share-button"
                            >
                                <WhatsappIcon size={26} round />
                            </WhatsappShareButton>
                        </div>
                    }
                    <div className="text-muted text-end">
                        <DateDisplay dateString={match.creationDateTime} />
                    </div>
                </div>
            </div>
            {!match.isLink &&
                <Link to={`/matches/${match.url}`} className="stretched-link"></Link>
            }
        </div>
    )
}