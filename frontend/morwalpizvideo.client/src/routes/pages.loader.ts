import { getPages } from "@services/pages";

export default async function loader({ params }) {
    const page = await getPages(params.pageId);
    return { page };
}