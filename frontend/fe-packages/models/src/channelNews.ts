export type ChannelNewsStatus = 'Draft' | 'Scheduled' | 'Published' | 'Archived' | number;

export type ChannelNewsImage = Readonly<{
  publicUrl: string;
  contentType: string;
  width: number;
  height: number;
  altText: string;
  displayOrder: number;
}>;

export type ChannelNews = Readonly<{
  id: string;
  slug: string;
  channelId: string;
  channelName: string;
  channelLogoUrl: string;
  title: string;
  subtitle: string;
  descriptionHtml: string;
  images: readonly ChannelNewsImage[];
  status: ChannelNewsStatus;
  publicationTimeUtc?: string;
}>;

export type ChannelNewsAdmin = Readonly<{
  id: string;
  channelId: string;
  title: string;
  subtitle: string;
  descriptionHtml: string;
  images: readonly ChannelNewsImage[];
  slug: string;
  status: ChannelNewsStatus;
  publicationTimeUtc?: string;
  displayOrder: number;
  creationDateTime: string;
  updatedDateTime: string;
}>;