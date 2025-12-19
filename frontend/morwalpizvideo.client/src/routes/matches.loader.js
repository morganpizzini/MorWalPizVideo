import { getMatch, getMatchImages } from "@services/matches";

export default async function loader({ params }) {
    const matchPromise = getMatch(params.matchId);
    const imagePromise = getMatchImages(params.matchId);
    const [match, images] = await Promise.all([matchPromise, imagePromise])
    console.log(match);
    console.log(images);
    return {
        match,
        images: images.map(s => ({
                                source: s,
                                title: 'Titolo',
                                description: 'Descrizione',
                            }))
    }
}
