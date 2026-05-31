import type { LoaderFunctionArgs } from "react-router";
import { getMatchLinktree, getMatch } from "@services/linktree";

export default async function loader({ params }: LoaderFunctionArgs) {
    const matchPromise = getMatch(params.matchId as string);
    const linktreePromise = getMatchLinktree(params.matchId as string);
    
    try {
        const [match, videoLinks] = await Promise.all([matchPromise, linktreePromise]);
        console.log('Match:', match);
        console.log('Video Links:', videoLinks);
        
        return {
            match,
            videoLinks: videoLinks || []
        };
    } catch (error) {
        console.error('Error loading linktree data:', error);
        throw new Response("Linktree not found", { status: 404 });
    }
}
