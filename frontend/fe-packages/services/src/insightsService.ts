import { get, post, put, Delete } from './apiService';
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
} from '@morwalpizvideo/models';

const BASE_URL = '/api/insights';

/**
 * Topics API
 */
export const insightsTopicsApi = {
  getAll: (): Promise<InsightTopic[]> => get(`${BASE_URL}/topics`),

  getById: (id: string): Promise<InsightTopic> => get(`${BASE_URL}/topics/${id}`),

  create: (data: CreateInsightTopicRequest): Promise<InsightTopic> =>
    post(`${BASE_URL}/topics`, data),

  update: (id: string, data: UpdateInsightTopicRequest): Promise<InsightTopic> =>
    put(`${BASE_URL}/topics/${id}`, data),

  delete: (id: string): Promise<void> => Delete(`${BASE_URL}/topics/${id}`),

  scanNews: (id: string): Promise<InsightNewsItem[]> =>
    post(`${BASE_URL}/topics/${id}/scan-news`, {}),

  getNews: (id: string, status?: InsightNewsStatus): Promise<InsightNewsItem[]> => {
    const url = status !== undefined ? `${BASE_URL}/topics/${id}/news` : `${BASE_URL}/topics/${id}/news`;
    return status !== undefined ? get(url, { status }) : get(url);
  },

  getContentPlans: (id: string): Promise<InsightContentPlan[]> =>
    get(`${BASE_URL}/topics/${id}/content-plans`),
};

/**
 * News Items API
 */
export const insightsNewsApi = {
  getAll: (): Promise<InsightNewsItem[]> => get(`${BASE_URL}/news`),

  getById: (id: string): Promise<InsightNewsItem> => get(`${BASE_URL}/news/${id}`),

  review: (id: string, data: ReviewNewsItemRequest): Promise<InsightNewsItem> =>
    put(`${BASE_URL}/news/${id}/review`, data),

  delete: (id: string): Promise<void> => Delete(`${BASE_URL}/news/${id}`),
};

/**
 * Content Plans API
 */
export const insightsContentPlansApi = {
  getAll: (): Promise<InsightContentPlan[]> => get(`${BASE_URL}/content-plans`),

  getById: (id: string): Promise<InsightContentPlan> =>
    get(`${BASE_URL}/content-plans/${id}`),

  generate: (data: GenerateContentPlanRequest): Promise<InsightContentPlan> =>
    post(`${BASE_URL}/content-plans`, data),

  update: (id: string, data: UpdateContentPlanRequest): Promise<InsightContentPlan> =>
    put(`${BASE_URL}/content-plans/${id}`, data),

  delete: (id: string): Promise<void> => Delete(`${BASE_URL}/content-plans/${id}`),
};