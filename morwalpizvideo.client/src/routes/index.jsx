import { Link, useLoaderData } from "react-router-dom";
import { getMatches } from "@services/matches";
import './index.scss'
export async function loader() {
    const matches = await getMatches();
    return { matches };
}
export default function Index() {
    const { matches } = useLoaderData();
    return (
        <>
            <div className="row">
                {matches.map(match => (
                    <div key={match.thumbnailUrl} className="col-md-6 col-lg-4 mb-3">
                        <div className="card position-relative">
                            <img src={`https://img.youtube.com/vi/${match.thumbnailUrl}/hqdefault.jpg`} className="card-img-top" alt="Video Thumbnail" />
                            <div className="card-body">
                                <h5 className="card-title">{match.title}</h5>
                                <p className="card-text">{match.description}</p>
                            </div>
                            <Link to={`/matches/${match.url}`} className="stretched-link"></Link>
                        </div>
                    </div>))}
            </div>
        </>
    );
}