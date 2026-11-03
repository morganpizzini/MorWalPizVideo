import type {
  AiExecutionRecord,
  AiPromptCanaryRequest,
  AiPromptDefinition,
  AiPromptExperiment,
  AiPromptRevision,
  AiPromptRollbackRequest,
  CreateAiEvaluationCaseRequest,
  CreateAiPromptRevisionRequest,
  EvaluateAiPromptRequest,
} from "@morwalpizvideo/models";
import { get, post, requireSuccessfulResponse } from "./apiService";

const workflowsEndpoint = "api/v1/ai/workflows";

const stageEndpoint = (workflowKey: string, stageKey: string) =>
  `${workflowsEndpoint}/${encodeURIComponent(workflowKey)}/${encodeURIComponent(stageKey)}`;

export const getAiPromptDefinitions = (): Promise<AiPromptDefinition[]> =>
  get(workflowsEndpoint);

export const getAiPromptRevisions = (
  workflowKey: string,
  stageKey: string,
): Promise<AiPromptRevision[]> =>
  get(`${stageEndpoint(workflowKey, stageKey)}/revisions`);

export const getAiPromptExperiments = (
  workflowKey: string,
  stageKey: string,
): Promise<AiPromptExperiment[]> =>
  get(`${stageEndpoint(workflowKey, stageKey)}/experiments`);

export const getAiExecutionRecords = (
  workflowKey: string,
  stageKey: string,
): Promise<AiExecutionRecord[]> =>
  get(`${stageEndpoint(workflowKey, stageKey)}/executions`, { take: 50 });

export const bootstrapAiPrompts = (): Promise<{ created: number }> =>
  post(`${workflowsEndpoint}/bootstrap`, {}).then(requireSuccessfulResponse);

export const createAiPromptRevision = (
  workflowKey: string,
  stageKey: string,
  request: CreateAiPromptRevisionRequest,
): Promise<AiPromptRevision> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/revisions`, request).then(
    requireSuccessfulResponse,
  );

export const createAiPromptCandidate = (
  experimentId: string,
): Promise<AiPromptRevision> =>
  post(
    `api/v1/ai/experiments/${encodeURIComponent(experimentId)}/candidate`,
    {},
  ).then(requireSuccessfulResponse);

export const createAiEvaluationCase = (
  workflowKey: string,
  stageKey: string,
  request: CreateAiEvaluationCaseRequest,
): Promise<{ id: string; datasetVersion: string; expiresAtUtc: string }> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/evaluation-cases`, {
    ...request,
    workflowKey,
    stageKey,
  }).then(requireSuccessfulResponse);

export const evaluateAiPrompt = (
  workflowKey: string,
  stageKey: string,
  request: EvaluateAiPromptRequest,
): Promise<AiPromptExperiment> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/evaluate`, request).then(
    requireSuccessfulResponse,
  );

export const startAiPromptCanary = (
  workflowKey: string,
  stageKey: string,
  request: AiPromptCanaryRequest,
): Promise<AiPromptDefinition> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/canary`, request).then(
    requireSuccessfulResponse,
  );

export const promoteAiPromptCanary = (
  workflowKey: string,
  stageKey: string,
  request: AiPromptCanaryRequest,
): Promise<AiPromptDefinition> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/canary/promote`, request).then(
    requireSuccessfulResponse,
  );

export const cancelAiPromptCanary = (
  workflowKey: string,
  stageKey: string,
  request: AiPromptCanaryRequest,
): Promise<AiPromptDefinition> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/canary/cancel`, request).then(
    requireSuccessfulResponse,
  );

export const rollbackAiPrompt = (
  workflowKey: string,
  stageKey: string,
  request: AiPromptRollbackRequest,
): Promise<AiPromptDefinition> =>
  post(`${stageEndpoint(workflowKey, stageKey)}/rollback`, request).then(
    requireSuccessfulResponse,
  );
