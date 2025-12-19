import { getMatches } from "@services/matches";
import { getConfiguration } from "@services/stream";
export default async function loader() {
    const responsePromise = getMatches();
    const configurationPromise = getConfiguration();
    const [response, configuration] = await Promise.all([
        responsePromise,configurationPromise
    ])
    return { matches: response.data, total: response.count, next: response.next, configuration: configuration };
}