import { Link, useLoaderData } from "react-router";
import React, { useMemo, useState } from "react";
import DateDisplay from "@utils/date-display";
import SEO from "@utils/seo";
import './index.scss'
import { FacebookShareButton, FacebookIcon, WhatsappShareButton, WhatsappIcon } from "react-share";
import ReactGA from "react-ga4"
import configKeys from "@utils/configKeys"
import Masonry, { ResponsiveMasonry } from "react-responsive-masonry"
export default function Index() {
    ReactGA.send({ hitType: 'pageview', page: window.location.pathname, title: "Home" })
    const { matches, configuration } = useLoaderData();

    const [selectedCategories, setSelectedCategories] = useState([]);

    let firstMatchId = matches[0];
    if (firstMatchId) {
        if (firstMatchId.videos?.length > 0) {
            firstMatchId = firstMatchId.videos[firstMatchId.videos.length - 1].id;
        } else {
            firstMatchId = firstMatchId.url
        }
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
                imageUrl={matches.length > 0 ? `https://img.youtube.com/vi/${matches[0].thumbnailUrl}/hqdefault.jpg` : ''}
                type='website' />
            {configuration[configKeys.STREAM_ENABLE] &&
                <>
                    <div className="alert alert-warning my-3 d-flex align-items-center justify-content-between" role="alert">
                        <div>
                            <i className="fa fa-circle-exclamation me-2"></i>
                            <strong>ATTENZIONE:</strong> Una diretta è attualmente in corso! Non perdertela!
                        </div>
                        <Link to="/stream" className="btn btn-warning ms-2">
                            Vai alla diretta <i className="fa fa-arrow-right ms-1"></i>
                        </Link>
                    </div>
                </>
            }
            <div className="row align-items-center">
                {matches.length > 0 &&
                    <>
                        <div className="d-none d-md-block col-md-3">
                            {RenderMatchCard(matches[0], -1)}
                        </div>
                        <div className="col-12 col-md-9">
                            <iframe width="100%" height="450px" className="rounded" src={`https://www.youtube.com/embed/${firstMatchId}?autoplay=1&mute=1`} title="YouTube video player" frameBorder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen></iframe>
                        </div>
                    </>
                }
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

            {renderContentWithBanners(filteredItems, selectedCategories)}
        </>
    );
}

function renderContentWithBanners(items, selectedCategories) {
    // Common configuration for all Masonry layouts
    const columnsCountBreakPoints = { 350: 1, 750: 2, 900: 3 };
    const gutterBreakpoints = { 350: "12px", 750: "16px", 900: "24px" };

    // Create initial section (before Banner)
    const firstSection = items.slice(0, 8);
    const middleSection = items.slice(8, 17);
    const lastSection = items.slice(17);

    const shouldShowBanners = selectedCategories.length === 0;

    return (
        <>
            {/* First section */}
            <ResponsiveMasonry
                columnsCountBreakPoints={columnsCountBreakPoints}
                gutterBreakpoints={gutterBreakpoints}
            >
                <Masonry>
                    {firstSection.map((match, i) => {
                        // Create an array of elements to render
                        const elementsToRender = [
                            // Always render the match card
                            <React.Fragment key={`match-${i}`}>
                                {RenderMatchCard(match, selectedCategories.length === 0 ? i : -1)}
                            </React.Fragment>
                        ];
                        if (i === 3) elementsToRender.push(<BuyMeACoffeeCard key={`coffee-${i}`} />);
                        if (i === 5) elementsToRender.push(<GoToShortsCard key={`shorts-${i}`} />);
                        // Return the flattened elements
                        return elementsToRender;
                    }).flat()}
                </Masonry>
            </ResponsiveMasonry>

            {shouldShowBanners && <Banner />}

            {/* Middle section */}
            {middleSection.length > 0 && (
                <ResponsiveMasonry
                    columnsCountBreakPoints={columnsCountBreakPoints}
                    gutterBreakpoints={gutterBreakpoints}
                >
                    <Masonry>
                        {middleSection.map((match, i) => (
                            <React.Fragment key={`match-${i + 7}`}>
                                {RenderMatchCard(match, shouldShowBanners ? i + 7 : -1)}
                            </React.Fragment>
                        ))}
                    </Masonry>
                </ResponsiveMasonry>
            )}

            {/* Sponsors full width */}
            {shouldShowBanners && <Sponsors />}

            {/* Last section */}
            {lastSection.length > 0 && (
                <ResponsiveMasonry
                    columnsCountBreakPoints={columnsCountBreakPoints}
                    gutterBreakpoints={gutterBreakpoints}
                >
                    <Masonry>
                        {lastSection.map((match, i) => (
                            <React.Fragment key={`match-${i + 15}`}>
                                {RenderMatchCard(match, shouldShowBanners ? i + 15 : -1)}
                            </React.Fragment>
                        ))}
                    </Masonry>
                </ResponsiveMasonry>
            )}
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
                    <img src="https://morwalpizblob.blob.core.windows.net/page-images/home/stories.jpg" alt="Stories" style={{ objectFit: "contain", "width": "100%" }} />
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
    const className = i == 0 ? "card position-relative d-md-none w-100" : "card position-relative w-100";
    const isLink = (match.videos == null && match.videoRefs == null);
    return (
        <div className={className} style={{ width: '100%' }}>
            <img src={`https://img.youtube.com/vi/${match.url}/hqdefault.jpg`} className="card-img-top" alt="Video Thumbnail" />
            <div className="card-body">
                {isLink &&
                    <p className="text-muted mb-1 text-uppercase">{match.category}</p>
                }
                {!isLink &&
                    <p className="text-muted mb-1 text-uppercase">{match.videos?.length} video</p>
                }
                <h5 className="card-title">{match.title}</h5>
                <p className="card-text">{match.description}</p>
                <div className="d-flex justify-content-between align-items-center">
                    {isLink &&
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
            {!isLink &&
                <Link to={`/matches/${match.url}`} className="stretched-link"></Link>
            }
        </div>
    )
}