import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  accountGet: vi.fn(),
  accountPost: vi.fn(),
  accountPut: vi.fn(),
  accountDelete: vi.fn(),
  adminGet: vi.fn(),
  adminPut: vi.fn(),
}));

vi.mock("./apiTransport", () => ({
  accountApiService: {
    get: mocks.accountGet,
    post: mocks.accountPost,
    put: mocks.accountPut,
    Delete: mocks.accountDelete,
  },
  adminApiService: {
    get: mocks.adminGet,
    put: mocks.adminPut,
  },
}));

import {
  getBlogComments,
  getPublicAccountInteractions,
  updatePublicAccountPortalAccess,
} from "./blogInteractionService";

describe("blog interaction API services", () => {
  beforeEach(() => vi.clearAllMocks());

  it("encodes article slugs and forwards comment pagination", async () => {
    await getBlogComments("article/with spaces", 2, 15);

    expect(mocks.accountGet).toHaveBeenCalledWith(
      "api/blog/article%2Fwith%20spaces/interactions/comments",
      { page: 2, pageSize: 15 },
    );
  });

  it("encodes account IDs and sends channel-scoped access revisions", async () => {
    await getPublicAccountInteractions("account/id", 3);
    await updatePublicAccountPortalAccess("account/id", {
      revision: 4,
      commentsBlocked: true,
      portalAccessRevoked: false,
      reason: "Moderation",
    });

    expect(mocks.adminGet).toHaveBeenCalledWith(
      "api/blog-interactions/accounts/account%2Fid",
      { page: 3, pageSize: 50 },
    );
    expect(mocks.adminPut).toHaveBeenCalledWith(
      "api/blog-interactions/accounts/account%2Fid/access",
      {
        revision: 4,
        commentsBlocked: true,
        portalAccessRevoked: false,
        reason: "Moderation",
      },
    );
  });
});
