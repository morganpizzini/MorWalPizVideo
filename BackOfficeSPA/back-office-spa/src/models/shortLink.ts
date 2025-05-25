/**
 * Rappresenta i tipi di link supportati
 */
export enum LinkType {
  YouTubeVideo = 0,
  YouTubeChannel = 1,
  YouTubePlaylist = 2,
  Instagram = 3,
  Facebook = 4,
  CustomUrl = 5
}

/**
 * Represents a short link in the application
 * @property shortLinkId - Unique identifier for the short link
 * @property target - Target of the link (video ID, channel ID, Instagram post ID, etc.)
 * @property linkType - Type of link (YouTube video, channel, Instagram, etc.)
 * @property queryString - Query parameters for the link
 * @property message - Optional message for the short link
 * @property clicksCount - Number of times the link has been clicked
 * @property videoId - Legacy property for backward compatibility
 */
export interface ShortLink {
  /** Unique identifier for the short link */
  shortLinkId: string;

  /** Target of the link (video ID, channel ID, Instagram post ID, etc.) */
  target: string;
  
  /** Type of the link */
  linkType: LinkType;

  /** Query parameters for the link */
  queryString: string;

  /** Optional message for the short link */
  message: string;

  /** Number of times the link has been clicked */
  clicksCount: number;
  
  /** Legacy property for backward compatibility */
  videoId: string;
}

/**
 * Type for creating a new short link (all fields required except id and clicksCount which may be generated)
 */
export type CreateShortLinkDTO = Omit<ShortLink, 'shortLinkId' | 'clicksCount' | 'videoId'> & {
  shortLinkId?: string;
  clicksCount?: string;
};

/**
 * Type for updating an existing short link (all fields optional except id)
 */
export type UpdateShortLinkDTO = Partial<
  Omit<ShortLink, 'shortLinkId' | 'clicksCount' | 'message' | 'videoId'>
> & {
  shortLinkId: string;
};
