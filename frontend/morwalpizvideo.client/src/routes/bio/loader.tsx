import { getBioLinks } from "@services/bioLinks";
export default async function loader() {
    const links = await getBioLinks();
    return { links };
}