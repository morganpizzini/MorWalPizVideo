export interface QueryLink {
    queryLinkId: string;
    title: string;
    value: string;
}
export type CreateQueryLinkDTO = Omit<QueryLink, 'queryLinkId'> & {
    queryLinkId?: string;
};
export type UpdateQueryLinkDTO = Partial<Omit<QueryLink, 'queryLinkId'>> & {
    queryLinkId: string;
};
//# sourceMappingURL=queryLink.d.ts.map