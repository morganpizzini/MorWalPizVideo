import { get, post, put, Delete, getFile } from './apiService';
import type {
  InsightTopic,
  InsightNewsItem,
  InsightContentPlan,
  CreateInsightTopicRequest,
  UpdateInsightTopicRequest,
  ReviewNewsItemRequest,
  GenerateContentPlanRequest,
  UpdateContentPlanRequest,
  InsightNewsStatus,
  AnalyzeInsightCommentsRequest,
  AnalyzeInsightCommentsResponse,
} from '@morwalpizvideo/models';

const BASE_URL = '/api/insights';

export function requireSuccessfulResponse<T>(response: T): T {
  const payload = response as T & { errors?: unknown[]; status?: number };
  if (payload && Array.isArray(payload.errors) && payload.errors.length > 0) {
    const message = payload.errors
      .map(error => typeof error === 'string' ? error : JSON.stringify(error))
      .join(', ') || 'The insights request failed.';
    throw new Error(message);
  }

  return response;
}

/**
 * Topics API
 */
export const insightsTopicsApi = {
  getAll: async (): Promise<InsightTopic[]> => requireSuccessfulResponse(await get(`${BASE_URL}/topics/admin`)),

  getById: async (id: string): Promise<InsightTopic> => requireSuccessfulResponse(await get(`${BASE_URL}/topics/${id}`)),

  create: async (data: CreateInsightTopicRequest): Promise<InsightTopic> =>
    requireSuccessfulResponse(await post(`${BASE_URL}/topics`, data)),

  update: async (id: string, data: UpdateInsightTopicRequest): Promise<InsightTopic> =>
    requireSuccessfulResponse(await put(`${BASE_URL}/topics/${id}`, data)),

  delete: async (id: string): Promise<void> => { requireSuccessfulResponse(await Delete(`${BASE_URL}/topics/${id}`)); },

  scanNews: async (id: string): Promise<InsightNewsItem[]> =>
    requireSuccessfulResponse(await post(`${BASE_URL}/topics/${id}/scan-news`, {})),

  getNews: (id: string, status?: InsightNewsStatus): Promise<InsightNewsItem[]> => {
    const url = status !== undefined ? `${BASE_URL}/topics/${id}/news` : `${BASE_URL}/topics/${id}/news`;
    return status !== undefined
      ? get(url, { status }).then(requireSuccessfulResponse)
      : get(url).then(requireSuccessfulResponse);
  },

  getContentPlans: (id: string): Promise<InsightContentPlan[]> =>
    get(`${BASE_URL}/topics/${id}/content-plans`).then(requireSuccessfulResponse),

  analyzeComments: (id: string, data: AnalyzeInsightCommentsRequest): Promise<AnalyzeInsightCommentsResponse> =>
    post(`${BASE_URL}/topics/${id}/analyze-comments`, data).then(requireSuccessfulResponse),

  getCommentAnalysisRun: (topicId: string, runId: string) =>
    get(`${BASE_URL}/topics/${topicId}/analyze-comments/${runId}`).then(requireSuccessfulResponse) as Promise<AnalyzeInsightCommentsResponse>,

  rescheduleCommentAnalysis: (topicId: string, runId: string) =>
    post(`${BASE_URL}/topics/${topicId}/analyze-comments/${runId}/reschedule`, {}).then(requireSuccessfulResponse) as Promise<AnalyzeInsightCommentsResponse>,

  exportCsv: (id: string): Promise<Blob> => getFile(`${BASE_URL}/topics/${id}/export`),
};

/**
 * News Items API
 */
export const insightsNewsApi = {
  getAll: (): Promise<InsightNewsItem[]> => get(`${BASE_URL}/news`).then(requireSuccessfulResponse),

  getById: (id: string): Promise<InsightNewsItem> => get(`${BASE_URL}/news/${id}`).then(requireSuccessfulResponse),

  review: (id: string, data: ReviewNewsItemRequest): Promise<InsightNewsItem> =>
    put(`${BASE_URL}/news/${id}/review`, data).then(requireSuccessfulResponse),

  delete: async (id: string): Promise<void> => { requireSuccessfulResponse(await Delete(`${BASE_URL}/news/${id}`)); },
};

/**
 * Content Plans API
 */
export const insightsContentPlansApi = {
  getAll: (): Promise<InsightContentPlan[]> => get(`${BASE_URL}/content-plans`).then(requireSuccessfulResponse),

  getById: (id: string): Promise<InsightContentPlan> =>
    get(`${BASE_URL}/content-plans/${id}`).then(requireSuccessfulResponse),

  generate: (data: GenerateContentPlanRequest): Promise<InsightContentPlan> =>
    post(`${BASE_URL}/content-plans`, data).then(requireSuccessfulResponse),

  update: (id: string, data: UpdateContentPlanRequest): Promise<InsightContentPlan> =>
    put(`${BASE_URL}/content-plans/${id}`, data).then(requireSuccessfulResponse),

  delete: async (id: string): Promise<void> => { requireSuccessfulResponse(await Delete(`${BASE_URL}/content-plans/${id}`)); },
};