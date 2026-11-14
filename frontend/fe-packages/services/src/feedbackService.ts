import type {
  FeedbackPage,
  FeedbackReceipt,
  FeedbackStatus,
  FeedbackItem,
  SubmitFeedbackRequest,
  UpdateFeedbackStatusRequest,
} from "@morwalpizvideo/models";
import endpoints, { ComposeUrl } from "./endpoints";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService, adminApiService, requireSuccessfulResponse } from "./apiTransport";

export async function submitFeedback(request: SubmitFeedbackRequest): Promise<FeedbackReceipt> {
  const response = await accountApiService.post(frontendEndpoints.PUBLIC_FEEDBACK, request);
  requireSuccessfulResponse(response);
  if (
    typeof response?.id !== "string" ||
    response.id.length === 0 ||
    typeof response.receivedAtUtc !== "string" ||
    response.receivedAtUtc.length === 0 ||
    typeof response.authenticated !== "boolean"
  ) {
    throw new Error("The feedback receipt was invalid");
  }
  return response as FeedbackReceipt;
}

export function getFeedbackPage(
  status?: FeedbackStatus,
  application?: string,
  page = 1,
  pageSize = 20,
): Promise<FeedbackPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (status) query.set("status", status);
  if (application) query.set("application", application);
  return adminApiService.get(`${endpoints.FEEDBACK}?${query.toString()}`)
    .then(requireSuccessfulResponse) as Promise<FeedbackPage>;
}

export function getFeedback(id: string): Promise<FeedbackItem> {
  return adminApiService.get(ComposeUrl(endpoints.FEEDBACK_DETAIL, { feedbackId: id }))
    .then(requireSuccessfulResponse) as Promise<FeedbackItem>;
}

export function updateFeedbackStatus(
  id: string,
  request: UpdateFeedbackStatusRequest,
): Promise<FeedbackItem> {
  return adminApiService.patch(ComposeUrl(endpoints.FEEDBACK_STATUS, { feedbackId: id }), request)
    .then(requireSuccessfulResponse) as Promise<FeedbackItem>;
}

export function deleteFeedback(id: string): Promise<unknown> {
  return adminApiService.Delete(ComposeUrl(endpoints.FEEDBACK_DETAIL, { feedbackId: id }))
    .then(requireSuccessfulResponse);
}
