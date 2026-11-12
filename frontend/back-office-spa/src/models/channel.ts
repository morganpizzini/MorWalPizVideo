/**
 * Represents a content channel in the application
 * @property channelId - Unique identifier for the channel
 * @property channelName - User-friendly name of the channel
 * @property yTChannelId - YouTube channel identifier
 */
export type Channel = Readonly<{
  /** Persisted channel document identifier; optional for legacy responses. */
  id?: string;

  /** Unique identifier for the channel */
  channelId: string;

  /** User-friendly name of the channel */
  channelName: string;

  /** YouTube channel identifier */
  yTChannelId: string;
  isSHIT?: boolean;
  publicBaseUrl?: string;
  youTubeStudioUrl?: string;
  googleAnalyticsUrl?: string;
  socials?: readonly ChannelSocial[];
}>;

export type ChannelSocial = Readonly<{ provider: string; handler: string }>;

/**
 * Type for creating a new channel (all fields required except id which may be generated)
 */
export type CreateChannelDTO = Omit<Channel, 'channelId' | 'id'> & {
  channelId?: string;
};

/**
 * Type for updating an existing channel (all fields optional except id)
 */
export type UpdateChannelDTO = Partial<Omit<Channel, 'channelId' | 'id'>> & {
  channelId: string;
};
