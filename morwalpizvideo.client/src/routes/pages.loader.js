import { useLoaderData } from "react-router"
import './pages.scss'
import SEO from "@utils/seo";
import { getPages } from "@services/pages";

export default async function loader({ params }) {
    const page = await getPages(params.pageId);
    return { page };
}