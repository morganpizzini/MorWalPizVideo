import { getCompilationByUrl } from "@services/compilations";

export default async function loader({ params }) {
    const compilation = await getCompilationByUrl(params.compilationUrl);
    return { compilation };
}
