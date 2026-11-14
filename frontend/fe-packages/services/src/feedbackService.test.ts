import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  accountPost: vi.fn(),
  adminGet: vi.fn(),
  adminPatch: vi.fn(),
  adminDelete: vi.fn(),
}));

vi.mock("./apiTransport", async () => {
  const actual = await vi.importActual<typeof import("./apiTransport")>("./apiTransport");
  return {
    ...actual,
    accountApiService: { post: mocks.accountPost },
    adminApiService: {
      get: mocks.adminGet,
      patch: mocks.adminPatch,
      Delete: mocks.adminDelete,
    },
  };
});

import { deleteFeedback, getFeedback, getFeedbackPage, submitFeedback, updateFeedbackStatus } from "./feedbackService";

describe("feedback transport", () => {
  beforeEach(() => vi.clearAllMocks());

  it("accepts only a complete successful receipt", async () => {
    mocks.accountPost.mockResolvedValue({
      id: "f1",
      receivedAtUtc: "2026-01-01T00:00:00Z",
      authenticated: false,
    });
    await expect(submitFeedback({} as never)).resolves.toMatchObject({ id: "f1" });

    mocks.accountPost.mockResolvedValue({ id: "f1", authenticated: false });
    await expect(submitFeedback({} as never)).rejects.toThrow("receipt was invalid");
  });

  it.each([
    ["list", () => getFeedbackPage(), mocks.adminGet],
    ["detail", () => getFeedback("f1"), mocks.adminGet],
    ["status", () => updateFeedbackStatus("f1", { status: "Resolved", expectedRevision: 1 }), mocks.adminPatch],
    ["delete", () => deleteFeedback("f1"), mocks.adminDelete],
  ])("rejects unsuccessful %s responses", async (_name, operation, mock) => {
    mock.mockResolvedValue({ status: 500, errors: ["failed"] });
    await expect(operation()).rejects.toThrow("failed");
  });
});
