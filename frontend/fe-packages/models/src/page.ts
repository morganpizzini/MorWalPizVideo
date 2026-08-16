export const PageStatus = {
  Draft: 0,
  Published: 1,
} as const;

export type PageStatus = typeof PageStatus[keyof typeof PageStatus];

export interface PageImage {
  publicUrl: string;
  contentType: string;
  width: number;
  height: number;
  altText: string;
}

export interface PageAdmin {
  id: string;
  channelId: string;
  thumbnailUrl: string;
  title: string;
  content: string;
  url: string;
  videoId: string;
  status: PageStatus;
  inlineImages: PageImage[];
  creationDateTime: string;
  updatedDateTime: string;
}

export interface PagePublic {
  thumbnailUrl: string;
  title: string;
  content: string;
  url: string;
  videoId: string;
  inlineImages: PageImage[];
}

export interface CreatePageDTO {
  thumbnailUrl: string;
  title: string;
  content: string;
  url: string;
  videoId: string;
  status: PageStatus;
}

export type UpdatePageDTO = CreatePageDTO;