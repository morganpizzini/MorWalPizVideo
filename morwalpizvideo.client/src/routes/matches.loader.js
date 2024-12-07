import { getMatch } from "@services/matches";

export default async function loader({ params }) {
    const match = await getMatch(params.matchId);
    return { match };
}
