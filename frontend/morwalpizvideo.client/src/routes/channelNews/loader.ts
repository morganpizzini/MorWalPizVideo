import { getPublicChannelNewsByIdOrSlug } from "@morwalpizvideo/services";

export default async function loader({ params }: { params: Record<string, string | undefined> }) {
    if (!params.idOrSlug) throw new Response("ChannelNews not found", { status: 404 });
    return getPublicChannelNewsByIdOrSlug(params.idOrSlug);
}