import { getMatches } from "@services/matches";
import { getConfiguration } from "@services/stream";
import { getActiveForms } from "@services/customForms";
import { getSponsors } from "@services/sponsors";
import { getPublicChannelNews } from "@morwalpizvideo/services";

interface SponsorItem {
    title: string;
    imgSrc: string;
    url: string;
}

export default async function loader() {
    try {
        const [response, configuration, activeForms, channelNews, sponsors] = await Promise.all([
            getMatches(true),
            getConfiguration(),
            getActiveForms(),
            getPublicChannelNews(),
            getSponsors().catch(() => [] as SponsorItem[])
        ]);

        return {
            matches: response.data ?? [],
            total: response.count,
            next: response.next,
            configuration: configuration ?? {},
            activeForms: activeForms ?? [],
            channelNews: channelNews ?? [],
            sponsors: sponsors ?? [],
            error: false
        };
    } catch {
        return {
            matches: [],
            total: 0,
            next: undefined,
            configuration: {},
            activeForms: [],
            channelNews: [],
            sponsors: [],
            error: true
        };
    }
}
