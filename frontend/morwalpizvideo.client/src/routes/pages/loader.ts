import type { LoaderFunctionArgs } from "react-router";
import { getPages } from "@services/pages";

export default async function loader({ params }: LoaderFunctionArgs) {
    const page = await getPages(params.pageId as string);
    return { page };
}