export type EditorialProductionCardStatus =
  | "Idea"
  | "Planned"
  | "InProgress"
  | "Ready"
  | "Published"
  | "Archived"
  | number;

export type EditorialProductionCard = Readonly<{
  id: string;
  channelId: string;
  title: string;
  editorialAngle: string;
  sourceUrls: readonly string[];
  brief: string;
  script: string;
  status: EditorialProductionCardStatus;
  ownerUserId?: string;
  dueDateUtc?: string;
  topicId?: string;
  newsItemId?: string;
  contentPlanId?: string;
  youtubeVideoId?: string;
  backOfficeContentId?: string;
  revision: number;
  creationDateTime: string;
  updatedDateTime: string;
}>;

export type EditorialProductionCardRequest = Omit<
  EditorialProductionCard,
  "id" | "channelId" | "revision" | "creationDateTime" | "updatedDateTime"
> & { revision?: number };
