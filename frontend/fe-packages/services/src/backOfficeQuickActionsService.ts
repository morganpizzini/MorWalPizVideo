import type {
  QuickActionsResponse,
  SaveQuickActionsRequest,
} from "@morwalpizvideo/models";
import endpoints from "./endpoints";
import { get, put, Delete, requireSuccessfulResponse } from "./apiTransport";

export const getDashboardQuickActions = (): Promise<QuickActionsResponse> =>
  get(endpoints.DASHBOARD_QUICK_ACTIONS).then(requireSuccessfulResponse);

export const saveDashboardQuickActions = (
  request: SaveQuickActionsRequest,
): Promise<QuickActionsResponse> =>
  put(endpoints.DASHBOARD_QUICK_ACTIONS, request).then(requireSuccessfulResponse);

export const resetDashboardQuickActions = (): Promise<QuickActionsResponse> =>
  Delete(endpoints.DASHBOARD_QUICK_ACTIONS).then(requireSuccessfulResponse);
