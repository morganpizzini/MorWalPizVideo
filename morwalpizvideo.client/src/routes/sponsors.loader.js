import { getSponsors } from "@services/sponsors";
import { data } from "react-router";
export default async function loader() {
    const sponsors = await getSponsors();
    if (!sponsors) {
        // throw to ErrorBoundary
        throw data(null, { status: 404 });
    }
    return { sponsors };
}