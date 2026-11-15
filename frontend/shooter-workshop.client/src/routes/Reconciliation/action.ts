import { rebuildWorkshopProjections } from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";

export async function action(_args: ActionFunctionArgs) {
  try {
    await rebuildWorkshopProjections();
    return { ok: true as const };
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "The request could not be completed.",
    };
  }
}
