import type { LoaderFunctionArgs } from "react-router";
import { getMatch, getMatchImages } from "@services/matches";

export default async function loader({ params }: LoaderFunctionArgs) {
    const matchPromise = getMatch(params.matchId as string);
    const imagePromise = getMatchImages(params.matchId as string);
    const [match, images] = await Promise.all([matchPromise, imagePromise])
    console.log(match);
    console.log(images);
    return {
        match,
        images: (images as string[]).map((s: string) => ({
                                source: s,
                                title: 'Titolo',
                                description: 'Descrizione',
                            }))
    }
}
