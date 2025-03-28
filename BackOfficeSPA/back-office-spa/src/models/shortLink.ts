/**
 * Represents a short link in the application
 * @property shortLinkId - Unique identifier for the short link
 * @property videoId - ID of the associated video
 * @property queryString - Query parameters for the link
 * @property message - Optional message for the short link
 * @property clicksCount - Number of times the link has been clicked
 */
export interface ShortLink {
  /** Unique identifier for the short link */
  shortLinkId: string;

  /** ID of the associated video */
  videoId: string;

  /** Query parameters for the link */
  queryString: string;

  /** Optional message for the short link */
  message: string;

  /** Number of times the link has been clicked */
  clicksCount: number;
}

/**
 * Type for creating a new short link (all fields required except id and clicksCount which may be generated)
 */
export type CreateShortLinkDTO = Omit<ShortLink, 'shortLinkId' | 'clicksCount'> & {
  shortLinkId?: string;
  clicksCount?: string;
};

/**
 * Type for updating an existing short link (all fields optional except id)
 */
export type UpdateShortLinkDTO = Partial<
  Omit<ShortLink, 'shortLinkId' | 'clicksCount' | 'message'>
> & {
  shortLinkId: string;
};
