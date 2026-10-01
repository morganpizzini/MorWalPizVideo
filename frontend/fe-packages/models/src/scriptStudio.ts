export type ScriptStudioOperation =
  | "expand"
  | "rewrite"
  | "structure"
  | "prettify";
export type ScriptStudioFormat = "plain" | "markdown";

export interface ScriptStudioDocument {
  channelId: string;
  script: string;
  savedPrompt: string;
  examples: string;
  style: string;
  generalContext: string;
  savedResult: string;
  format: ScriptStudioFormat;
}

export interface ScriptStudioGenerationRequest {
  operation: ScriptStudioOperation;
  script: string;
  prompt: string;
  examples: string;
  style: string;
  generalContext: string;
  format: ScriptStudioFormat;
}

export interface ScriptStudioGenerationResponse {
  result: string;
  format: ScriptStudioFormat;
  quotaLimit: number;
  quotaUsed: number;
}

export interface ScriptStudioGlobalPrompt {
  prompt: string;
  version: number;
}
