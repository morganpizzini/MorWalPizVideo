import type { ActionFunctionArgs } from "react-router";
import { action as recipesAction } from "../Recipes/action";
import { submitProduction } from "../submitProduction";

export async function action(args: ActionFunctionArgs) {
  const { request } = args;
  const actionRequest = request.clone();
  const actionForm = await actionRequest.formData();
  const intent = actionForm.get("intent");
  if (intent === "create" || intent === "update") {
    return recipesAction(args);
  }
  return submitProduction(actionForm);
}
