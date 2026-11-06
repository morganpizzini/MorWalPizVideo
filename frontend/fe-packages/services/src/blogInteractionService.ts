import type {
  BlogAdminComment,
  BlogAdminCommentPage,
  BlogArticleInteractionsPage,
  BlogComment,
  BlogCommentPage,
  BlogCommentSubmission,
  BlogInteractionSummary,
  BlogPortalAccess,
  BlogPortalAccessRequest,
  BlogReactionRequest,
  BlogViewerState,
  PublicAccountDirectoryPage,
  PublicAccountInteractionDetail,
} from "@morwalpizvideo/models";
import endpoints from "./endpoints";
import frontendEndpoints from "./endpoints-frontend";
import { accountApiService, adminApiService } from "./apiTransport";

const publicInteractionPath = (slug: string) =>
  frontendEndpoints.PUBLIC_BLOG_INTERACTIONS.replace(
    "{slug}",
    encodeURIComponent(slug),
  );

export const getBlogInteractionSummary = (
  slug: string,
): Promise<BlogInteractionSummary> =>
  accountApiService.get(`${publicInteractionPath(slug)}/summary`);

export const getBlogComments = (
  slug: string,
  page = 1,
  pageSize = 20,
): Promise<BlogCommentPage> =>
  accountApiService.get(`${publicInteractionPath(slug)}/comments`, {
    page,
    pageSize,
  });

export const getBlogViewerState = (slug: string): Promise<BlogViewerState> =>
  accountApiService.get(`${publicInteractionPath(slug)}/me`);

export const submitBlogComment = (
  slug: string,
  request: BlogCommentSubmission,
): Promise<BlogComment> =>
  accountApiService.post(`${publicInteractionPath(slug)}/comments`, request);

export const setBlogReaction = (
  slug: string,
  request: BlogReactionRequest,
): Promise<unknown> =>
  accountApiService.put(`${publicInteractionPath(slug)}/reaction`, request);

export const clearBlogReaction = (slug: string): Promise<unknown> =>
  accountApiService.Delete(`${publicInteractionPath(slug)}/reaction`);

export const withdrawBlogComment = (
  slug: string,
  id: string,
): Promise<unknown> =>
  accountApiService.Delete(
    `${publicInteractionPath(slug)}/comments/${encodeURIComponent(id)}`,
  );

export const getPublicAccountDirectory = (
  search = "",
  page = 1,
  pageSize = 50,
): Promise<PublicAccountDirectoryPage> =>
  adminApiService.get(endpoints.PUBLIC_ACCOUNTS, { search, page, pageSize });

export const getPublicAccountInteractions = (
  accountId: string,
  page = 1,
  pageSize = 50,
): Promise<PublicAccountInteractionDetail> =>
  adminApiService.get(
    endpoints.PUBLIC_ACCOUNT_INTERACTIONS.replace(
      "{accountId}",
      encodeURIComponent(accountId),
    ),
    { page, pageSize },
  );

export const updatePublicAccountPortalAccess = (
  accountId: string,
  request: BlogPortalAccessRequest,
): Promise<BlogPortalAccess> =>
  adminApiService.put(
    endpoints.PUBLIC_ACCOUNT_PORTAL_ACCESS.replace(
      "{accountId}",
      encodeURIComponent(accountId),
    ),
    request,
  );

export const getBlogAdminComments = (
  status?: number,
  postId?: string,
  page = 1,
  pageSize = 50,
): Promise<BlogAdminCommentPage> =>
  adminApiService.get(endpoints.BLOG_INTERACTION_COMMENTS, {
    ...(status === undefined ? {} : { status }),
    ...(postId ? { postId } : {}),
    page,
    pageSize,
  });

export const getBlogArticleInteractions = (
  page = 1,
  pageSize = 50,
): Promise<BlogArticleInteractionsPage> =>
  adminApiService.get(endpoints.BLOG_INTERACTION_ARTICLES, { page, pageSize });

export const moderateBlogComment = (
  comment: Pick<BlogAdminComment, "id" | "revision">,
  status: number,
): Promise<BlogAdminComment> =>
  adminApiService.put(
    `${endpoints.BLOG_INTERACTION_COMMENTS}/${encodeURIComponent(comment.id)}`,
    {
      revision: comment.revision,
      status,
    },
  );
