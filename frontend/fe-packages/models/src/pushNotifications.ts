/** Shared Web Push contracts. Mirrors MorWalPiz.Contracts/Contracts/PushContracts.cs. */

export type PushSubscriptionKeysRequest = Readonly<{
  p256dh: string;
  auth: string;
}>;

export type PushSubscribeRequest = Readonly<{
  endpoint: string;
  keys: PushSubscriptionKeysRequest;
  channelIds: readonly string[];
  applicationKey: string;
  language?: string;
  /** Returned once on first subscribe; required to change or revoke an existing endpoint. */
  credential?: string;
}>;

export type PushSubscriptionCredentialRequest = Readonly<{
  endpoint: string;
  credential: string;
}>;

/** Anonymous subscription state. The raw push endpoint is deliberately never returned. */
export type PushSubscriptionState = Readonly<{
  channelIds: readonly string[];
  credential?: string;
}>;

export type PushPublicKey = Readonly<{
  publicKey: string;
}>;

export type PushDispatchScope = 'Platform' | 'Channel' | number;

export type PushDispatchState =
  | 'Draft'
  | 'Queued'
  | 'Sending'
  | 'Sent'
  | 'Failed'
  | 'Cancelled'
  | number;

export type PushAudience = Readonly<{
  id: string;
  code: string;
  name: string;
  description: string;
  channelIds: readonly string[];
  isActive: boolean;
  creationDateTime: string;
  updatedAt?: string;
}>;

export type PushAudienceRequest = Readonly<{
  code: string;
  name: string;
  description?: string;
  channelIds: readonly string[];
  isActive?: boolean;
}>;

export type PushTargetChannel = Readonly<{
  channelId: string;
  channelName: string;
  activeSubscriberCount: number;
}>;

export type PushTargets = Readonly<{
  channels: readonly PushTargetChannel[];
  audiences: readonly PushAudience[];
  /** Server-side cap on action buttons; browsers apply their own Notification.maxActions on top. */
  maxActions: number;
}>;

export type PushNotificationActionRequest = Readonly<{
  action: string;
  title: string;
  /** Same-origin relative path. */
  destination: string;
}>;

export type PushPlatformSendRequest = Readonly<{
  title: string;
  body: string;
  destination?: string;
  actions?: readonly PushNotificationActionRequest[];
  channelIds?: readonly string[];
  audienceIds?: readonly string[];
  allChannels?: boolean;
  templateId?: string;
}>;

export type PushChannelSendRequest = Readonly<{
  title: string;
  body: string;
  destination?: string;
  actions?: readonly PushNotificationActionRequest[];
  templateId?: string;
}>;

export type PushDispatch = Readonly<{
  id: string;
  scope: PushDispatchScope;
  state: PushDispatchState;
  title: string;
  body: string;
  destination: string;
  actions: readonly PushNotificationActionRequest[];
  channelIds: readonly string[];
  audienceIds: readonly string[];
  ownerChannelId: string;
  recipientCount: number;
  creationDateTime: string;
  queuedAt?: string;
  completedAt?: string;
}>;

export type PushNotificationTemplate = Readonly<{
  id: string;
  name: string;
  title: string;
  body: string;
  destination: string;
  actions: readonly PushNotificationActionRequest[];
  version: number;
  isActive: boolean;
  updatedAt: string;
}>;

export type PushNotificationTemplateRequest = Readonly<{
  name: string;
  title: string;
  body: string;
  destination?: string;
  actions?: readonly PushNotificationActionRequest[];
}>;
