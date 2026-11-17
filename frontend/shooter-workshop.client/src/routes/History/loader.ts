import { getWorkshopHistory } from "@morwalpizvideo/services";
import type { LoaderFunctionArgs } from "react-router";
import { deferRouteData } from "../shared";

export interface HistoryLoaderData {
  items: Awaited<ReturnType<typeof getWorkshopHistory>>;
  page: number;
  pageSize: number;
  type: string;
}

export function loader({ request }: LoaderFunctionArgs) {
  const searchParams = new URL(request.url).searchParams;
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = 50;
  const type = searchParams.get("type") ?? "";
  return deferRouteData(getWorkshopHistory({ page, pageSize, type: type || undefined }).then((items): HistoryLoaderData => ({
    items,
    page,
    pageSize,
    type,
  })));
}
