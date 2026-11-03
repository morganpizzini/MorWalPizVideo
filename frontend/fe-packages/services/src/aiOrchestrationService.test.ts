import { afterEach, describe, expect, it, vi } from "vitest";
import * as api from "./apiService";
import {
  createAiPromptRevision,
  getAiExecutionRecords,
  getAiPromptRevisions,
  startAiPromptCanary,
} from "./aiOrchestrationService";

describe("AI orchestration service", () => {
  afterEach(() => vi.restoreAllMocks());

  it("uses versioned workflow routes and encodes workflow keys", async () => {
    const get = vi.spyOn(api, "get").mockResolvedValue([]);

    await getAiPromptRevisions("chat/workflow", "metadata");

    expect(get).toHaveBeenCalledWith(
      "api/v1/ai/workflows/chat%2Fworkflow/metadata/revisions",
    );
  });

  it("requests only the bounded metadata execution history", async () => {
    const get = vi.spyOn(api, "get").mockResolvedValue([]);

    await getAiExecutionRecords("chat.workflow", "metadata");

    expect(get).toHaveBeenCalledWith(
      "api/v1/ai/workflows/chat.workflow/metadata/executions",
      { take: 50 },
    );
  });

  it("saves prompt revisions and canary requests through the shared API client", async () => {
    const post = vi.spyOn(api, "post").mockResolvedValue({ id: "revision-1" });
    const request = {
      messages: [
        { role: "system" as const, template: "Ground output in the source." },
        { role: "user" as const, template: "{{payload}}" },
      ],
      variables: ["payload"],
      responseSchema: "text",
      allowedToolNames: [],
      rationale: "Improve source grounding.",
    };

    await createAiPromptRevision("chat.workflow", "metadata", request);
    await startAiPromptCanary("chat.workflow", "metadata", {
      candidateRevisionId: "revision-1",
      expectedActiveRevisionId: "revision-0",
      experimentId: "experiment-1",
      percentage: 10,
    });

    expect(post).toHaveBeenNthCalledWith(
      1,
      "api/v1/ai/workflows/chat.workflow/metadata/revisions",
      request,
    );
    expect(post).toHaveBeenNthCalledWith(
      2,
      "api/v1/ai/workflows/chat.workflow/metadata/canary",
      {
        candidateRevisionId: "revision-1",
        expectedActiveRevisionId: "revision-0",
        experimentId: "experiment-1",
        percentage: 10,
      },
    );
  });
});
