import type {
  ScriptStudioDocument,
  ScriptStudioGenerationRequest,
  ScriptStudioGenerationResponse,
  ScriptStudioGlobalPrompt,
} from "@morwalpizvideo/models";
import endpoints from "./endpoints";
import { get, post, put, requireSuccessfulResponse } from "./apiService";

export const getScriptStudio = (): Promise<ScriptStudioDocument> =>
  get(endpoints.SCRIPT_STUDIO);
export const saveScriptStudio = (
  document: ScriptStudioDocument,
): Promise<ScriptStudioDocument> =>
  put(endpoints.SCRIPT_STUDIO, document).then(requireSuccessfulResponse);
export const generateScriptStudio = (
  request: ScriptStudioGenerationRequest,
): Promise<ScriptStudioGenerationResponse> =>
  post(endpoints.SCRIPT_STUDIO_GENERATE, request).then(
    requireSuccessfulResponse,
  );
export const getScriptStudioGlobalPrompt =
  (): Promise<ScriptStudioGlobalPrompt> =>
    get(endpoints.SCRIPT_STUDIO_GLOBAL_PROMPT);
export const saveScriptStudioGlobalPrompt = (
  prompt: string,
): Promise<ScriptStudioGlobalPrompt> =>
  put(endpoints.SCRIPT_STUDIO_GLOBAL_PROMPT, { prompt }).then(
    requireSuccessfulResponse,
  );
