/**
 * Represents a query link in the application
 * @property queryLinkId - Unique identifier for the query link
 * @property title - Title of the query link
 * @property description - Description of the query link
 */
export interface QueryLink {
  /** Unique identifier for the query link */
  queryLinkId: string;

  /** Title of the query link */
  title: string;

  /** Description of the query link */
  value: string;
}

/**
 * Type for creating a new query link (all fields required except id which may be generated)
 */
export type CreateQueryLinkDTO = Omit<QueryLink, 'queryLinkId'> & {
  queryLinkId?: string;
};

/**
 * Type for updating an existing query link (all fields optional except id)
 */
export type UpdateQueryLinkDTO = Partial<Omit<QueryLink, 'queryLinkId'>> & {
  queryLinkId: string;
};
