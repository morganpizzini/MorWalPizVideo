export interface BlogMark {
  type: "bold" | "italic" | "strike" | "code" | "link";
  href?: string | null;
}
export interface BlogText {
  type:
    | "doc"
    | "paragraph"
    | "text"
    | "bulletList"
    | "orderedList"
    | "listItem"
    | "blockquote"
    | "hardBreak";
  text?: string | null;
  marks?: BlogMark[];
  content?: BlogText[];
}
export type BlogBlockType =
  | "richText"
  | "heading"
  | "image"
  | "gallery"
  | "carousel"
  | "video"
  | "columns";
export interface BlogBlock {
  id: string;
  type: BlogBlockType;
  text?: string;
  level?: number;
  richText?: BlogText;
  imageIds?: string[];
  videoId?: string;
  columns?: BlogBlock[][];
}
export interface BlogDocument {
  version: 1;
  blocks: BlogBlock[];
}
export interface BlogSnapshot {
  title: string;
  summary: string;
  author: string;
  tags: string[];
  coverImageId: string | null;
  coverAlt: string;
  seoTitle: string;
  seoDescription: string;
  document: BlogDocument;
  updatedAt?: string;
}
export interface BlogImage {
  id: string;
  publicUrl: string;
  width: number;
  height: number;
  altText: string;
}
export interface BlogPostAdmin {
  id: string;
  slug: string;
  revision: number;
  draft: BlogSnapshot;
  isPublished: boolean;
  firstPublishedAt: string | null;
  publishedAt: string | null;
  images: BlogImage[];
}
export interface SaveBlogPost {
  slug: string;
  revision: number;
  draft: BlogSnapshot;
}
export interface BlogSummary {
  slug: string;
  title: string;
  summary: string;
  author: string;
  tags: string[];
  coverUrl: string | null;
  coverAlt: string;
  publishedAt: string;
}
export interface BlogPostPublic {
  metadata: BlogSummary;
  document: BlogDocument;
  seoTitle: string;
  seoDescription: string;
  updatedAt: string;
  images: BlogImage[];
}
export interface BlogPage {
  items: BlogSummary[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}
