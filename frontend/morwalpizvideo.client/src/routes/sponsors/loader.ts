import { getSponsors } from "@services/sponsors";
import { getCustomFormByUrl } from "@services/customForms";
import { data } from "react-router";
export default async function loader() {
    const formUrl = 'sponsor';
    const [sponsors, form] = await Promise.all([getSponsors(), getCustomFormByUrl(formUrl)]);
    if (!sponsors || !form) {
        // throw to ErrorBoundary
        throw data(null, { status: 404 });
    }
    return { sponsors, form };
}