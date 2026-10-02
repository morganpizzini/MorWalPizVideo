import type {
  BlogPage,
  BlogPostAdmin,
  BlogPostPublic,
  SaveBlogPost,
} from "@morwalpizvideo/models";
import {
  get,
  post,
  put,
  postFormData,
  requireSuccessfulResponse,
} from "./apiService";
import { publicGet } from "./apiTransport";
import endpoints, { ComposeUrl } from "./endpoints";

const detail = (id: string) => ComposeUrl(endpoints.BLOG_POST_DETAIL, { id });
export const fetchBlogPosts = async (page = 1): Promise<BlogPostAdmin[]> =>
  requireSuccessfulResponse(await get(endpoints.BLOG_POSTS, { page }));
export const getBlogPost = async (id: string): Promise<BlogPostAdmin> =>
  requireSuccessfulResponse(await get(detail(id)));
export const createBlogPost = async (
  payload: SaveBlogPost,
): Promise<BlogPostAdmin> =>
  requireSuccessfulResponse(await post(endpoints.BLOG_POSTS, payload));
export const saveBlogPost = async (
  id: string,
  payload: SaveBlogPost,
): Promise<BlogPostAdmin> =>
  requireSuccessfulResponse(await put(detail(id), payload));
export const publishBlogPost = async (
  id: string,
  revision: number,
  publish: boolean,
): Promise<BlogPostAdmin> =>
  requireSuccessfulResponse(
    await post(`${detail(id)}/${publish ? "publish" : "unpublish"}`, {
      revision,
    }),
  );
export const uploadBlogImage = async (
  id: string,
  revision: number,
  file: File,
  altText: string,
): Promise<BlogPostAdmin> => {
  const form = new FormData();
  form.append("file", file);
  form.append("revision", String(revision));
  form.append("altText", altText);
  return requireSuccessfulResponse(
    await postFormData(`${detail(id)}/images`, form),
  );
};
export const getPublicBlog = async (page = 1): Promise<BlogPage> =>
  requireSuccessfulResponse(await publicGet(endpoints.BLOG, { page }));
export const getPublicBlogPost = async (
  slug: string,
): Promise<BlogPostPublic> =>
  requireSuccessfulResponse(
    await publicGet(ComposeUrl(endpoints.BLOG_DETAIL, { slug })),
  );
