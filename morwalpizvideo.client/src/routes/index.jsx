import { Link, useLoaderData } from "react-router-dom";
import { getMatches } from "@services/matches";
import DateDisplay from "@utils/date-display";
import './index.scss'
export async function loader() {
    const matches = await getMatches();
    return { matches };
}
export default function Index() {
    const { matches } = useLoaderData();
    return (
        <>
            <div className="card-columns">
                {matches.map((match,i )=> (
                    <>
                    <div className="card position-relative" key={match.thumbnailUrl}>
                        <img src={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`} className="card-img-top" alt="Video Thumbnail" />
                        <div className="card-body">
                            <h5 className="card-title">{match.title}</h5>
                            <p className="card-text">{match.description}</p>
                            {match.isLink &&
                                <p className="text-muted">{match.category}</p>
                            }
                            <div className="d-flex justify-content-between align-items-center">
                                <span>
                                    {match.isLink &&
                                        <i className="fa-2x text-danger fab fa-youtube"></i>
                                    }
                                </span>
                                <div className="text-muted text-end">
                                    <DateDisplay dateString={match.creationDateTime} />
                                </div>
                            </div>
                        </div>
                        {match.isLink &&
                            <Link to={`https://youtu.be/${match.url}`} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                        }
                        {!match.isLink &&
                            <Link to={`/matches/${match.url}`} className="stretched-link"></Link>
                        }
                        </div>
                        {i === 5 &&
                            <Banner/>
                        }
                    </>
                ))}
            </div>
            {matches.length < 6 && <Banner />}
        </>
    );
}
function Banner() {
    return (
        <Link to={`/attrezzatura`} className="text-decoration-none text-black">
            <div className="alert alert-secondary my-3 text-center fw-bold pop-up" role="alert">
                Tutta la mia attrezzatura <i className="fa fa-arrow-right"></i>
            </div>
        </Link>
    )
}