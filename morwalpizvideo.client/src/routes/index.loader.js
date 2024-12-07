import { getMatches } from "@services/matches";
export default async function loader() {
    const matches = await getMatches();
    return { matches };
}