/**
 * Represents a content channel in the application
 * @property channelId - Unique identifier for the channel
 * @property channelName - User-friendly name of the channel
 * @property yTChannelId - YouTube channel identifier
 */
export type Channel = Readonly<{
  /** Unique identifier for the channel */
  channelId: string;

  /** User-friendly name of the channel */
  channelName: string;

  /** YouTube channel identifier */
  yTChannelId: string;
  isSHIT?: boolean;
  channelLogoUrl?: string;
  mine: boolean;
  shortLinkUrl?: string;
  socials?: readonly ChannelSocial[];
  socialPublishing?: ChannelSocialPublishing;
  videos?: readonly ChannelVideo[];
}>;

export type ChannelVideo = Readonly<{ videoId: string; title: string; lastCommentDate?: string }>;

export type ChannelSocial = Readonly<{ provider: string; handler: string }>;

export type ChannelSocialPublishing = Readonly<{
  telegram: ChannelSocialPublishingProvider;
  discord: ChannelSocialPublishingProvider;
  facebook: ChannelSocialPublishingProvider;
}>;

export type ChannelSocialPublishingProvider = Readonly<{
  destinationId: string;
  credentialConfigured: boolean;
}>;

export type TerminologyMapping = { source: string; target: string };
export type ChannelTerminology = {
  italianToEnglish: TerminologyMapping[];
  invariantEnglish: TerminologyMapping[];
};

/**
 * Type for creating a new channel (all fields required except id which may be generated)
 */
export type CreateChannelDTO = Omit<Channel, 'channelId'> & {
  channelId?: string;
};

/**
 * Type for updating an existing channel (all fields optional except id)
 */
export type UpdateChannelDTO = Partial<Omit<Channel, 'channelId' | 'yTChannelId'>> & {
  channelId: string;
};
