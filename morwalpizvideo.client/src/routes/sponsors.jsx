import { Link, useLoaderData } from "react-router-dom";
import { getSponsors } from "@services/sponsors";

export async function loader() {
    const sponsors = await getSponsors();
    return { sponsors };
}
export default function Accessories() {
    const { sponsors } = useLoaderData();

    return (
        <>
            <h1 className="text-center mb-3">SPONSORS</h1>
            <div className="row text-center">
                {sponsors.map(sponsor => <div key={sponsor.title} className="col-12 col-sm-6 col-md-4 position-relative">
                    <img className="mw-100" src={`images/sponsors/${sponsor.imgSrc}`} />
                    <Link to={sponsor.url} target="_blank" rel="noopener noreferrer" className="stretched-link"></Link>
                </div>)}
            </div>
        </>
    );
}