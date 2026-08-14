export const QuickLinkKind = {
  External: 0,
  Telegram: 1,
  Instagram: 2,
  Facebook: 3,
  Video: 4,
} as const;

export type QuickLinkKind = typeof QuickLinkKind[keyof typeof QuickLinkKind];

export interface QuickLink {
  kind: QuickLinkKind;
  targetUrl: string;
  title?: string;
  subtitle?: string;
  label?: string;
  imageUrl?: string;
  icon?: string;
  provider?: string;
}

export interface QuickLinks {
  id?: string;
  channelId?: string;
  title: string;
  subtitle?: string;
  url: string;
  links: QuickLink[];
  creationDateTime?: string;
}

export interface CreateQuickLinksDTO {
  channelId?: string;
  title: string;
  subtitle?: string;
  url: string;
  links: QuickLink[];
}

export type UpdateQuickLinksDTO = CreateQuickLinksDTO;
