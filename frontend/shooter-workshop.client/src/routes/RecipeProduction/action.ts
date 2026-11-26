import type { ActionFunctionArgs } from "react-router";
import { submitProduction } from "../submitProduction";

export async function action({ request }: ActionFunctionArgs) {
  return submitProduction(await request.formData());
}
