import { getConfiguration } from "@services/stream";
import { data } from "react-router";
export default async function loader() {
    const configuration = await getConfiguration();
    if (!configuration) {
        // throw to ErrorBoundary
        throw data(null, { status: 404 });
    }
    return { configuration };
}