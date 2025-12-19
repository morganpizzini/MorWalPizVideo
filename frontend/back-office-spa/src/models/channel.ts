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
}>;

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
