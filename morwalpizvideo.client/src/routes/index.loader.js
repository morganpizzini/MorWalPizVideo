import { getMatches } from "@services/matches";
export default async function loader() {
    const response = await getMatches();
    return { matches: response.data, total: response.count, next: response.next };
}