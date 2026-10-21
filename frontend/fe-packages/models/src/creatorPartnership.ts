export enum PartnerStatus {
  Prospect = 0,
  Active = 1,
  Paused = 2,
  Archived = 3,
}

export enum CampaignStatus {
  Draft = 0,
  Planned = 1,
  Active = 2,
  Completed = 3,
  Archived = 4,
}

export enum TimelineEntryKind {
  Note = 0,
  Contact = 1,
  Meeting = 2,
  Deliverable = 3,
  Content = 4,
  Payment = 5,
}

export interface Partner {
  id: string;
  channelId: string;
  name: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  websiteUrl: string;
  notes: string;
  status: PartnerStatus;
  updatedAt: string;
}

export interface Campaign {
  id: string;
  channelId: string;
  partnerId: string;
  title: string;
  description: string;
  status: CampaignStatus;
  startAt: string | null;
  endAt: string | null;
  contentReferences: string[];
  timelineEntries: TimelineEntry[];
  updatedAt: string;
}

export interface TimelineEntry {
  id: string;
  kind: TimelineEntryKind;
  title: string;
  description: string;
  occurredAt: string | null;
  dueAt: string | null;
  url: string;
  isComplete: boolean;
}

export interface CreatePartnerDTO {
  name: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  websiteUrl: string;
  notes: string;
  status: PartnerStatus;
}

export type UpdatePartnerDTO = CreatePartnerDTO;

export interface CreateCampaignDTO {
  partnerId: string;
  title: string;
  description: string;
  status: CampaignStatus;
  startAt: string | null;
  endAt: string | null;
  contentReferences: string[];
}

export type UpdateCampaignDTO = CreateCampaignDTO;

export interface CreateTimelineEntryDTO {
  kind: TimelineEntryKind;
  title: string;
  description: string;
  occurredAt: string | null;
  dueAt: string | null;
  url: string;
  isComplete: boolean;
}

export type UpdateTimelineEntryDTO = CreateTimelineEntryDTO;
