import { correctWorkshopShooting, recordWorkshopShooting } from "@morwalpizvideo/services";
import type { ActionFunctionArgs } from "react-router";
import { action as maintenanceAction } from "../Maintenance/action";
import { getRememberedAccountId, shootingSnapshot } from "../../services/offlineStore";
import { runOrQueueOffline } from "../../services/offlineMutations";

const text = (form: FormData, key: string) => {
  const value = form.get(key);
  return typeof value === "string" ? value.trim() : "";
};

function json<T>(form: FormData, key: string, fallback: T): T {
  const value = text(form, key);
  if (!value) return fallback;
  try {
    return JSON.parse(value) as T;
  } catch {
    throw new Error(`Invalid ${key}.`);
  }
}

export async function action(args: ActionFunctionArgs) {
  const { request } = args;
  const actionRequest = request.clone();
  const actionForm = await actionRequest.formData();
  if (text(actionForm, "intent") === "maintenance") {
    return maintenanceAction(args);
  }
  const form = await request.formData();
  const intent = text(form, "intent");
  try {
    if (intent === "record") {
      const firearmId = text(form, "firearmId");
      const date = text(form, "date");
      const totalRounds = Number(text(form, "totalRounds"));
      const untrackedAmmunition = Number(text(form, "untrackedAmmunition") || 0);
      if (!firearmId || !date || !Number.isInteger(totalRounds) || totalRounds < 1 ||
          !Number.isInteger(untrackedAmmunition) || untrackedAmmunition < 0) {
        return { ok: false as const, error: "Please review the shooting form." };
      }
      const request = {
        firearmId,
        date: new Date(date).toISOString(),
        totalRounds,
        notes: text(form, "notes") || null,
        ammunitionRecords: json(form, "ammunitionRecords", []),
        untrackedAmmunition,
        usageSegments: json(form, "usageSegments", []),
        idempotencyKey: text(form, "idempotencyKey") || crypto.randomUUID(),
      };
      const accountId = getRememberedAccountId() ?? "";
      const localSessionId = `local:${crypto.randomUUID()}`;
      const result = await runOrQueueOffline(
        () => recordWorkshopShooting(request),
        "shooting-create",
        { request },
        accountId
          ? { sessions: { upsert: [shootingSnapshot(accountId, localSessionId, request)] } }
          : undefined,
      );
      if (result.offline) return { ok: true as const, intent, offline: true };
      return { ok: true as const, intent };
    }

    if (intent === "correct") {
      const originalOperationId = text(form, "originalOperationId");
      if (!originalOperationId) return { ok: false as const, error: "The original session is missing." };
      await correctWorkshopShooting({
        originalOperationId,
        replacementShooting: json(form, "replacementShooting", null),
        idempotencyKey: text(form, "idempotencyKey") || crypto.randomUUID(),
      });
      return { ok: true as const, intent };
    }

    throw new Error("Unsupported shooting action.");
  } catch (error) {
    return {
      ok: false as const,
      error: error instanceof Error ? error.message : "The request could not be completed.",
    };
  }
}
