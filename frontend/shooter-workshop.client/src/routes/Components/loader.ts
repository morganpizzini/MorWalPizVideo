import { redirect, type LoaderFunctionArgs } from "react-router";

export function loader({ request }: LoaderFunctionArgs) {
  const target = new URL(request.url);
  target.pathname = "/firearms";
  target.searchParams.set("tab", "inventory");
  return redirect(`${target.pathname}?${target.searchParams.toString()}`);
}
