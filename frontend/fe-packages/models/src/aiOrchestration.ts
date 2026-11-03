export interface AiPromptMessage {
  role: "system" | "user" | "assistant";
  template: string;
}

export interface AiPromptDefinition {
  workflowKey: string;
  stageKey: string;
  displayName: string;
  responseSchema: string;
  activeRevisionId?: string | null;
  previousRevisionId?: string | null;
  canaryRevisionId?: string | null;
  canaryPercentage: number;
  canaryExperimentId?: string | null;
  canaryStartedAtUtc?: string | null;
  allowedToolNames: string[];
}

export interface AiPromptRevision {
  id: string;
  workflowKey: string;
  stageKey: string;
  messages: AiPromptMessage[];
  variables: string[];
  responseSchema: string;
  allowedToolNames: string[];
  parentRevisionId?: string | null;
  sourceExperimentId?: string | null;
  rationale: string;
  authorId: string;
  contentHash: string;
  lifecycle: string;
  createdAtUtc: string;
}

export interface AiPromptExperiment {
  id: string;
  workflowKey: string;
  stageKey: string;
  baselineRevisionId: string;
  candidateRevisionId: string;
  datasetVersion: string;
  scoringPolicyVersion: string;
  status: string;
  createdBy: string;
  createdAtUtc: string;
  casesEvaluated: number;
  baselinePassCount: number;
  candidatePassCount: number;
  baselineDurationMilliseconds: number;
  candidateDurationMilliseconds: number;
}

export interface AiExecutionRecord {
  executionId: string;
  workflowKey: string;
  stageKey: string;
  revisionId: string;
  channelId?: string | null;
  status: string;
  errorCode?: string | null;
  durationMilliseconds: number;
  inputCount: number;
  outputCount: number;
  createdAtUtc: string;
}

export interface CreateAiPromptRevisionRequest {
  messages: AiPromptMessage[];
  variables: string[];
  responseSchema: string;
  allowedToolNames: string[];
  rationale: string;
}

export interface AiPromptCanaryRequest {
  candidateRevisionId: string;
  expectedActiveRevisionId: string;
  expectedCanaryRevisionId?: string;
  experimentId: string;
  percentage?: number;
}

export interface AiPromptRollbackRequest {
  expectedActiveRevisionId: string;
}

export interface CreateAiEvaluationCaseRequest {
  datasetVersion: string;
  inputJson: string;
  expectedContains: string[];
  sourceClassification: string;
  contentConsentGranted: boolean;
  contentRedacted: boolean;
}

export interface EvaluateAiPromptRequest {
  candidateRevisionId: string;
  datasetVersion: string;
  caseIds: string[];
}
