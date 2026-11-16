import { getWorkshopHistory } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";

export async function loader({ request }: LoaderFunctionArgs) {
  const searchParams = new URL(request.url).searchParams;
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = 50;
  const type = searchParams.get("type") ?? "";
  return {
    items: await getWorkshopHistory({ page, pageSize, type: type || undefined }),
    page,
    pageSize,
    type,
  };
}
