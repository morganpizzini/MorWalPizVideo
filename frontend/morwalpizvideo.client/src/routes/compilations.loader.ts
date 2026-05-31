import type { LoaderFunctionArgs } from "react-router";
import { getCompilationByUrl } from "@services/compilations";

export default async function loader({ params }: LoaderFunctionArgs) {
    const compilation = await getCompilationByUrl(params.compilationUrl as string);
    return { compilation };
}
