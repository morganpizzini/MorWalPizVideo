import { Link, useLoaderData } from "react-router";
import React, { useMemo, useState } from "react";
import DateDisplay from "@utils/date-display";
import SEO from "@utils/seo";
import './index.scss'
import { FacebookShareButton, FacebookIcon, WhatsappShareButton, WhatsappIcon } from "react-share";
import ReactGA from "react-ga4"
export default function Index() {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: "Home" })
    const { matches,count } = useLoaderData();

    const [selectedCategories, setSelectedCategories] = useState([]);

    let firstMatchId = matches[0];
    if (firstMatchId.videos?.length > 0) {
        firstMatchId = firstMatchId.videos[firstMatchId.videos.length - 1].id;
    } else {
        firstMatchId = firstMatchId.url
    }

    const toggleCategory = (category) => {
        setSelectedCategories((prev) =>
            prev.includes(category)
                ? prev.filter((cat) => cat !== category)
                : [...prev, category]
        );
    };

    const filteredItems = useMemo(() => {
        if (selectedCategories.length === 0) return matches;
        return matches.filter((item) => {
            const itemCategories = item.category.split(",").map((cat) => cat.trim());
            return selectedCategories.every((selectedCategory) =>
                itemCategories.includes(selectedCategory)
            );
        });
    }, [matches, selectedCategories]);

    const availableCategories = useMemo(() => {
        if (selectedCategories.length === 0) {
            // Tutte le categorie sono disponibili se non ci sono filtri attivi
            const allCategories = matches.flatMap((item) =>
                item.category.split(",").map((cat) => cat.trim())
            );
            return [...new Set(allCategories)];
        }

        // Determina le categorie presenti negli oggetti filtrati
        const remainingCategories = filteredItems.flatMap((item) =>
            item.category.split(",").map((cat) => cat.trim())
        );
        return [...new Set(remainingCategories)];
    }, [filteredItems, matches, selectedCategories]);

    const allCategories = useMemo(() => {
        const all = matches.flatMap((item) =>
            item.category.split(",").map((cat) => cat.trim())
        );
        return [...new Set(all)];
    }, [matches]);

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
            <div className="my-3 p-2 bg-white rounded categories-container" style={{ display: "flex", gap: "10px" }}>
                {allCategories.map((category) => {
                    const includeCategory = availableCategories.includes(category);
                    return (
                        <button
                            key={category}
                            className={`btn ${selectedCategories.includes(category)
                                ? "btn-success"
                                : "btn-outline-secondary"}`}
                            onClick={() => toggleCategory(category)}
                            style={{
                                opacity: includeCategory ? 1 : 0.5,
                                marginRight: "10px",
                                cursor: includeCategory
                                    ? "pointer"
                                    : "not-allowed",
                            }}
                            disabled={!includeCategory}
                        >
                            {category}
                        </button>
                    )
                })}
            </div>

            <div className="card-columns">
                {filteredItems.map((match, i) => (
                    <React.Fragment key={i}>
                        {RenderMatchCard(match, selectedCategories.length == 0 ? i : -1)}
                        {selectedCategories == 0 && <>
                            {i === 3 &&
                                <BuyMeACoffeeCard />}
                            {i === 5 &&
                                <GoToShortsCard />}
                            {i === 6 &&
                                <Banner />}
                            {i === 14 &&
                                <Sponsors />}
                        </>}

                    </React.Fragment>
                ))}
            </div>
            {matches.length < 7 && <Banner />}
            {matches.length < 15 && <Sponsors />}
        </>
    );
}

function Banner() {
    return (
        <Link to={`/attrezzatura`} className="text-decoration-none text-black d-block" style={{ "columnSpan": "all" }}>
            <div className="alert alert-secondary my-3 text-center fw-bold pop-up text-uppercase" role="alert">
                La mia attrezzatura <i className="fa fa-arrow-right"></i>
            </div>
        </Link>
    )
}

function Sponsors() {
    return (
        <Link to={`/sponsors`} className="text-decoration-none text-black d-block" style={{ "columnSpan": "all" }}>
            <div className="alert alert-secondary my-3 text-center fw-bold pop-up text-uppercase" role="alert">
                I miei sponsors <i className="fa fa-arrow-right"></i>
            </div>
        </Link>
    )
}

function BuyMeACoffeeCard() {
    return (
        <>
            <div className="card position-relative">
                <div className="px-2" style={{ "heigth": "200px" }}>
                    <img src="https://morwalpizblob.blob.core.windows.net/page-images/home/buyme-button.png" alt="Buy Me A Coffee" style={{ objectFit: "contain", "width": "100%" }} />
                </div>
                <div className="card-body">
                    <p className="text-muted mb-1 text-uppercase">supporto</p>
                    <h5 className="card-title">Aiutami nel mio percorso!</h5>
                    <p className="card-text">Se ti fa piacere, offrimi l&#39;equivalente di un caricatore, o iscriverti per avere i contenuti in anteprima e una chat diretta!</p>
                </div>
                <Link to="https://www.buymeacoffee.com/MorWalPiz" target="_blank" rel="noopener noreferrer" className="stretched-link">
                </Link>
            </div>

        </>)
}

function GoToShortsCard() {
    return (
        <>
            <div className="card position-relative">
                <div className="px-2" style={{ "heigth": "200px" }}>
                    <img src="https://morwalpizblob.blob.core.windows.net/page-images/home/stories.jpg" alt="Buy Me A Coffee" style={{ objectFit: "contain", "width": "100%" }} />
                </div>
                <div className="card-body">
                    <p className="text-muted mb-1 text-uppercase">CONSIGLI</p>
                    <h5 className="card-title">Poco tempo? Ci sono gli SHORTS!</h5>
                    <p className="card-text">Guarda tutti i momenti salienti estratti dai miei video!</p>
                </div>
                <Link to="https://www.youtube.com/@morwalpiz/shorts" target="_blank" rel="noopener noreferrer" className="stretched-link">
                </Link>
            </div>

        </>)
}
function RenderMatchCard(match, i) {
    const className = i == 0 ? "card position-relative d-md-none" : "card position-relative";
    return (
        <div className={className}>
            <img src={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`} className="card-img-top" alt="Video Thumbnail" />
            <div className="card-body">
                {match.isLink &&
                    <p className="text-muted mb-1 text-uppercase">{match.category}</p>
                }
                {!match.isLink &&
                    <p className="text-muted mb-1 text-uppercase">{match.videos.length} video</p>
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